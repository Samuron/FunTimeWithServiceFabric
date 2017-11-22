using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Wirex.Engine;
using System.Reactive.Linq;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace Wirex.TradingService
{
    internal sealed class TradingService : StatefulService, ITradingService
    {
        public TradingService(StatefulServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(this.CreateServiceRemotingListener),
            };
        }

        public async Task PlaceOrderAsync(PlaceOrderRequest request)
        {
            var queue = await StateManager.GetOrAddAsync<IReliableQueue<OrderSnapshot>>("queue");
            var state = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, OrderSnapshot>>("state");

            var snapshot = new OrderSnapshot
            {
                Id = request.Id,
                BaseCurrency = request.BaseCurrency,
                QuoteCurrency = request.QuoteCurrency,
                Side = request.Side,
                Price = request.Price,
                Amount = request.Amount,
                Remaining = request.Amount
            };

            using (var transaction = StateManager.CreateTransaction())
            {    
                await queue.EnqueueAsync(transaction, snapshot);
                await state.AddAsync(transaction, snapshot.Id, snapshot);
                await transaction.CommitAsync();
            }
        }

        public async Task<long> GetOpenOrdersCount()
        {
            var state = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, OrderSnapshot>>("state");
            using (var transaction = StateManager.CreateTransaction())
            {
                var count = await state.GetCountAsync(transaction);
                return count;
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var queue = await StateManager.GetOrAddAsync<IReliableQueue<OrderSnapshot>>("queue");
            var state = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, OrderSnapshot>>("state");

            while (!cancellationToken.IsCancellationRequested)
            {
                using (var transaction = StateManager.CreateTransaction())
                {
                    var request = await queue.TryDequeueAsync(transaction);
                    if (!request.HasValue)
                    {
                        await transaction.CommitAsync();
                        continue;
                    }

                    var value = request.Value;
                    var order = Order.Restore(value);
                    var initial = order.GetSnapshot();

                    var potentialMatches = await state.CreateEnumerableAsync(transaction);

                    var enumerator = potentialMatches.GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {
                        var potentialMatch = enumerator.Current.Value;
                        var potentialOrder = Order.Restore(potentialMatch);
                        if (order.TryMatch(potentialOrder))
                        {
                            await PersistChanges(order, initial, state, transaction);
                            await PersistChanges(potentialOrder, potentialMatch,  state, transaction);
                            break;
                        }
                    }
                    
                    ServiceEventSource.Current.ServiceMessage(Context, "Order {0} was placed", request.Value);
                    await transaction.CommitAsync();
                }
            }
        }

        private async Task PersistChanges(Order order, OrderSnapshot initial, IReliableDictionary<Guid, OrderSnapshot> dictionary, ITransaction transaction)
        {
            var updated = await dictionary.TryUpdateAsync(transaction, order.Id, order.GetSnapshot(), initial);

            if (updated && order.IsClosed())
            {
                await dictionary.TryRemoveAsync(transaction, order.Id);
            }
        }
    }
}
