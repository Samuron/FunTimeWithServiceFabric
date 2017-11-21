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

            using (var transaction = StateManager.CreateTransaction())
            {
                await orders.EnqueueAsync(transaction, request);
                await transaction.CommitAsync();
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var ordersQueue = await StateManager.GetOrAddAsync<IReliableQueue<PlaceOrderRequest>>("orders");
            var ordersState = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, Order>>("ordersState");

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

                    var potentialMatches = await ordersState.CreateEnumerableAsync(transaction);

                    var enumerator = potentialMatches.GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {
                        var potentialMatch = enumerator.Current.Value;
                        if (order.TryMatch(potentialMatch))
                        {
                            await PersistChanges(order, ordersState, transaction);
                            await PersistChanges(potentialMatch, ordersState, transaction);
                            break;
                        }
                    }
                    
                    ServiceEventSource.Current.ServiceMessage(Context, "Order {0} was placed", request.Value);
                    await transaction.CommitAsync();
                }
            }
        }

        private Task PersistChanges(Order order, IReliableDictionary<Guid, Order> dictionary, ITransaction transaction)
        {
            return order.IsClosed() ? dictionary.TryRemoveAsync(transaction, order.Id) : dictionary.SetAsync(transaction, order.Id, order);
        }
    }
}
