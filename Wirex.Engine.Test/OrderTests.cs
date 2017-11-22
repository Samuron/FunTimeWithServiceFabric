using NUnit.Framework;
using System;

namespace Wirex.Engine.Test
{
    [TestFixture]
    public class OrderTests
    {
        [TestCase(1.1, 1.1)]
        [TestCase(2.5, 2.0)]
        public void OrdersWithSameCurrencyPairDifferentSidesAndBuyPriceGreaterOrEqualThanSellPriceShouldMatch(double buyPrice, double sellPrice)
        {
            // Arrange
            var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, (decimal)buyPrice, 50);
            var order2 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Sell, (decimal)sellPrice, 60);

            // Act
            var isMatch = order1.TryMatch(order2);

            // Assert
            Assert.That(isMatch);
            Assert.That(order1.GetSnapshot().Remaining, Is.EqualTo(0.0m));
            Assert.That(order2.GetSnapshot().Remaining, Is.EqualTo(10.0m));
        }

        [TestCase(2.5, 2.0)]
        public void OrdersWithSameCurrencyPairDifferentSidesAndSellPriceLowerThanBuyPriceShouldMatch(double buyPrice, double sellPrice)
        {
            // Arrange
            var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, (decimal)buyPrice, 60);
            var order2 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Sell, (decimal)sellPrice, 50);

            // Act
            var isMatch = order2.TryMatch(order1);

            // Assert
            Assert.That(isMatch);
            Assert.That(order1.GetSnapshot().Remaining, Is.EqualTo(10.0m));
            Assert.That(order2.GetSnapshot().Remaining, Is.EqualTo(0.0m));
        }

        [Test]
        public void OrderWithDifferentCurrencyPairShouldNotMatch()
        {
            // Arrange
            var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, 0.6m, 50);
            var order2 = new Order(Guid.NewGuid(), new CurrencyPair("EUR", "USD"), Side.Sell, 0.5m, 50);

            // Act
            var isMatch1 = order1.TryMatch(order2);
            var isMatch2 = order2.TryMatch(order1);

            // Assert
            Assert.That(isMatch1, Is.False);
            Assert.That(isMatch2, Is.False);
        }

        [TestCase(Side.Buy)]
        [TestCase(Side.Sell)]
        public void OrderWithSameSideShouldNotMatch(Side side)
        {
            // Arrange
            var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), side, 0.6m, 50);
            var order2 = new Order(Guid.NewGuid(), new CurrencyPair("EUR", "USD"), side, 0.5m, 50);

            // Act
            var isMatch1 = order1.TryMatch(order2);
            var isMatch2 = order2.TryMatch(order1);

            // Assert
            Assert.That(isMatch1, Is.False);
            Assert.That(isMatch2, Is.False);
        }

        [Test]
        public void OrderWithBuyPriceSmallerThanSellPriceShouldNotMatch()
        {
            // Arrange
            var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, 0.5m, 50);
            var order2 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Sell, 0.6m, 50);

            // Act
            var isMatch = order1.TryMatch(order2);

            // Assert
            Assert.That(isMatch, Is.False);
        }

        [Test]
        public void OrderWithSellPriceGreaterThanBuyPriceShouldNotMatch()
        {
            // Arrange
            var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, 0.5m, 50);
            var order2 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Sell, 0.6m, 50);

            // Act
            var isMatch = order2.TryMatch(order1);

            // Assert
            Assert.That(isMatch, Is.False);
        }

        [Test]
        public void CanCloseOrder()
        {
            var order1 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Buy, 1m, 50);
            var order2 = new Order(Guid.NewGuid(), new CurrencyPair("USD", "EUR"), Side.Sell, 1m, 50);

            Assert.That(order1.IsClosed, Is.False);
            Assert.That(order2.IsClosed, Is.False);

            order1.TryMatch(order2);

            Assert.That(order1.IsClosed, Is.True);
            Assert.That(order2.IsClosed, Is.True);
        }
    }
}
