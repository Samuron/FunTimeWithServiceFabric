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
using System.Reactive;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace Wirex.TradingService
{
    internal sealed class TradingService : StatefulService, ITradingService
    {
        private TradingEngine _tradingEngine;

        public TradingService(StatefulServiceContext context)
            : base(context)
        {
            _tradingEngine = new TradingEngine();
        }

        private async Task<Order> RemoveClosedOrder(EventPattern<OrderArgs> arg)
        {
            var order = arg.EventArgs.Order;
            var orders = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Order>>("orders");

            using (var transaction = StateManager.CreateTransaction())
            {
                await orders.EnqueueAsync(transaction, order);
                await transaction.CommitAsync();
            }

            return order;
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
            var orders = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<PlaceOrderRequest>>("orders");

            using (var transaction = StateManager.CreateTransaction())
            {
                await orders.EnqueueAsync(transaction, request);
                await transaction.CommitAsync();
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var orders = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<PlaceOrderRequest>>("orders");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var transaction = StateManager.CreateTransaction())
                {
                    var request = await orders.TryDequeueAsync(transaction);

                    if (!request.HasValue)
                    {
                        await transaction.CommitAsync();
                        continue;
                    }
                    var value = request.Value;
                    var currencyPair = new CurrencyPair(value.BaseCurrency, value.QuoteCurrency);
                    var order = new Order(value.Id, currencyPair, value.Side, value.Price, value.Amount);

                    _tradingEngine.Place(order);
                    ServiceEventSource.Current.ServiceMessage(Context, "Order {0} was placed", request.Value);
                    await transaction.CommitAsync();
                }
            }
        }
    }
}
