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
            var orders = await StateManager.GetOrAddAsync<IReliableQueue<PlaceOrderRequest>>("orders");
            var ordersState = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, OrderSnapshot>>("ordersState");

            using (var transaction = StateManager.CreateTransaction())
            {
                await orders.EnqueueAsync(transaction, request);
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
                await ordersState.AddAsync(transaction, request.Id, snapshot);
                await transaction.CommitAsync();
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var ordersQueue = await StateManager.GetOrAddAsync<IReliableQueue<PlaceOrderRequest>>("orders");
            var ordersState = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, OrderSnapshot>>("ordersState");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var transaction = StateManager.CreateTransaction())
                {
                    var request = await ordersQueue.TryDequeueAsync(transaction);
                    if (!request.HasValue)
                    {
                        await transaction.CommitAsync();
                        continue;
                    }

                    var value = request.Value;

                    var currencyPair = new CurrencyPair(value.BaseCurrency, value.QuoteCurrency);
                    var order = new Order(value.Id, currencyPair, value.Side, value.Price, value.Amount);
                    var initalState = order.GetSnapshot();

                    

                    var potentialMatches = await ordersState.CreateEnumerableAsync(transaction);

                    var enumerator = potentialMatches.GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {
                        var potentialMatch = enumerator.Current.Value;
                        var potentialOrder = Order.Restore(potentialMatch);
                        if (order.TryMatch(potentialOrder))
                        {
                            await PersistChanges(order, initalState, ordersState, transaction);
                            await PersistChanges(potentialOrder, potentialMatch,  ordersState, transaction);
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
            if (order.IsClosed())
            {
                // TODO: Consider soft deletes with background cleanup to avoid race conditions
                await dictionary.TryRemoveAsync(transaction, order.Id);
            }
            else
            {
                await dictionary.TryUpdateAsync(transaction, order.Id, order.GetSnapshot(), initial);
            }
        }
    }
}
