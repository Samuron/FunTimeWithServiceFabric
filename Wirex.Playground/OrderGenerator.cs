using System;
using System.Collections.Generic;
using Troschuetz.Random;
using Wirex.Engine;

namespace Wirex.Playground
{
    public class OrderGenerator
    {
        public static IEnumerable<Order> Generate(int orderCount, string baseCurrency, string quoteCurrency, double minPrice, double maxPrice)
        {
            var price = new TRandom();
            var amount = new TRandom();

            var currencyPair = new CurrencyPair(baseCurrency, quoteCurrency);

            for (int i = 0; i < orderCount; i++)
            {
                yield return new Order(Guid.NewGuid(), currencyPair, Side.Buy, decimal.Round((decimal)price.NextDouble(minPrice, maxPrice), 4), amount.Next(1, 100));
                yield return new Order(Guid.NewGuid(), currencyPair, Side.Sell, decimal.Round((decimal)price.NextDouble(minPrice, maxPrice), 4), amount.Next(1, 100));
            }
        }
    }
}