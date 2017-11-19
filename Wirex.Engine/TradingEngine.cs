using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wirex.Engine
{
    public class TradingEngine : ITradingEngine,
        IDisposable
    {
        private readonly BlockingCollection<Order> _backpressure;
        private readonly Task _consumer;
        private readonly List<Order> _orders;

        public TradingEngine()
        {
            _backpressure = new BlockingCollection<Order>();
            _orders = new List<Order>();
            _consumer = Task.Run(() => Consume());
        }

        public void Dispose()
        {
            _backpressure.CompleteAdding();
            _consumer.Wait();
            _backpressure.Dispose();
        }

        public void Place(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            _backpressure.Add(order);
            OrderOpened?.Invoke(this, new OrderArgs(order));
        }

        public event EventHandler<OrderArgs> OrderOpened;

        public event EventHandler<OrderArgs> OrderClosed;

        private void Consume()
        {
            foreach (var order in _backpressure.GetConsumingEnumerable())
            {
                _orders.Add(order);

                foreach (var potentialMatch in _orders)
                {
                    if (order.TryMatch(potentialMatch))
                    {
                        NotifyIfClosed(order);
                        NotifyIfClosed(potentialMatch);
                        break;
                    }
                }
            }
        }

        private void NotifyIfClosed(Order order)
        {
            if (order.IsClosed())
            {
                _orders.Remove(order);
                OrderClosed?.Invoke(this, new OrderArgs(order));
            }
        }
    }
}