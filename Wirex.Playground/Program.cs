using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using ShellProgressBar;
using Wirex.Engine;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace Wirex.Playground
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var orderCount = 100_000;

            StartRemoteProcessing(orderCount / 1000).Wait();

            Console.WriteLine("Press <Enter> to exit");
            Console.ReadLine();
        }

        private static async Task StartRemoteProcessing(int orderCount)
        {
            Console.WriteLine("Starting remote processing");
            var orders = OrderGenerator.Generate(orderCount, "USD", "EUR", 0.93, 0.99);

            var options = new ProgressBarOptions
            {
                DisplayTimeInRealTime = false,
                ForeGroundColor = ConsoleColor.Yellow,
                ForeGroundColorDone = ConsoleColor.DarkGreen,
                ProgressCharacter = '\u2593',
                CollapseWhenFinished = true
            };

            var address = new Uri("fabric:/Wirex.Service/Wirex.TradingService");
            var client = ServiceProxy.Create<ITradingService>(address, new ServicePartitionKey(1), targetReplicaSelector: TargetReplicaSelector.PrimaryReplica);

            var timer = new Stopwatch();
            using (var progressBar = new ProgressBar(orderCount, "Placing orders on remote server", options))
            {
                timer.Start();
                foreach (var order in orders)
                {
                    await client.PlaceOrderAsync(new PlaceOrderRequest
                    {
                        Id = order.Id,
                        Amount = order.Amount,
                        Price = order.Price,
                        Side = order.Side,
                        BaseCurrency = order.CurrencyPair.BaseCurrency,
                        QuoteCurrency = order.CurrencyPair.QuoteCurrency
                    });
                    progressBar.Tick($"Order {order} was placed");
                }
            }
            timer.Stop();
            Console.WriteLine();
            Console.WriteLine($"Processed {orderCount} orders in {timer.ElapsedMilliseconds} ms, average time: {timer.ElapsedMilliseconds / (double)orderCount} ms");
        }

        public static void StartDirectProcessing(int orderCount)
        {
            Console.WriteLine("Starting direct processing");
            var orders = OrderGenerator.Generate(orderCount, "USD", "EUR", 0.93, 0.99);

            var options = new ProgressBarOptions
            {
                DisplayTimeInRealTime = false,
                ForeGroundColor = ConsoleColor.Yellow,
                ForeGroundColorDone = ConsoleColor.DarkGreen,
                ProgressCharacter = '\u2593',
                CollapseWhenFinished = true
            };

            var timer = new Stopwatch();
            using (var engine = new TradingEngine())
            using (var progressBar = new ProgressBar(orderCount, "Closing orders", options))
            {
                engine.OrderClosed += (sender, orderArgs) =>
                {
                    // despite being pretty this is extremely slow, 
                    // do not treat this application as any kind of benchmark
                    // or at least comment the line below :)
                    progressBar.Tick($"Processing {orderArgs.Order}");
                };
                timer.Start();
                Parallel.ForEach(orders, (order, state, i) =>
                {
                    engine.Place(order);
                });
            }
            timer.Stop();
            Console.WriteLine();
            Console.WriteLine($"Processed {orderCount} orders in {timer.ElapsedMilliseconds} ms, average time: {timer.ElapsedMilliseconds / (double)orderCount} ms");
        }
    }
}