using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using System;

namespace Wirex.Engine.Test
{
    [TestFixture]
    public class TradingEngineTests
    {
        [Test]
        public void CanCloseBuyOrder()
        {
            using (var tradingEngine = new TradingEngine())
            {
                var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, 1.2m, 50m);
                var order2 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Sell, 1m, 60m);

                tradingEngine.OrderClosed += (s, e) =>
                {
                    Assert.That(e.Order, Is.EqualTo(order1));

                    Assert.That(order1.RemainingAmount, Is.EqualTo(0.00m));
                    Assert.That(order2.RemainingAmount, Is.EqualTo(10.0m));
                };

                tradingEngine.Place(order1);
                tradingEngine.Place(order2);
            }
        }

        [Test]
        public void CanCloseSellOrder()
        {
            using (var tradingEngine = new TradingEngine())
            {
                var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Sell, 1m, 50m);
                var order2 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, 1.2m, 60m);

                tradingEngine.OrderClosed += (s, e) =>
                {
                    Assert.That(e.Order, Is.EqualTo(order1));

                    Assert.That(order1.RemainingAmount, Is.EqualTo(0.00m));
                    Assert.That(order2.RemainingAmount, Is.EqualTo(10.0m));
                };

                tradingEngine.Place(order1);
                tradingEngine.Place(order2);
            }
        }

        [Test]
        public void PlaceOrderShouldRaiseOrderOpenedEvent()
        {
            using (var tradingEngine = new TradingEngine())
            {
                var order = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, 9.6m, 50);

                tradingEngine.OrderOpened += (s, e) =>
                {
                    Assert.That(e.Order, Is.EqualTo(order));
                };

                tradingEngine.Place(order);
            }
        }

        [Test]
        [Repeat(10)]
        public void NaiveConcurrencyTest()
        {
            var iterations = 1_000_000;
            var orderPlacedCounter = 0;
            var orderClosedCounter = 0;

            using (var tradingEngine = new TradingEngine())
            {
                tradingEngine.OrderOpened += (s, e) =>
                {
                    Interlocked.Increment(ref orderPlacedCounter);
                };
                tradingEngine.OrderClosed += (s, e) =>
                {
                    Interlocked.Increment(ref orderClosedCounter);
                };

                var pair = new CurrencyPair("USD", "EUR");
                Parallel.For(0, iterations, i =>
                {
                    var order = i % 2 == 0 ? new Order(Guid.NewGuid(), pair, Side.Sell, 9.5m, 50) : new Order(Guid.NewGuid(), pair, Side.Buy, 9.6m, 50);

                    tradingEngine.Place(order);
                });
            }

            Assert.That(orderPlacedCounter, Is.EqualTo(iterations));
            Assert.That(orderClosedCounter, Is.EqualTo(iterations));
        }
    }
}