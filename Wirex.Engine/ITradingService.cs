using System;
using System.Threading.Tasks;

namespace Wirex.Engine
{
    public interface ITradingService : Microsoft.ServiceFabric.Services.Remoting.IService
    {
        Task PlaceOrderAsync(PlaceOrderRequest request);

        Task<long> GetOpenOrdersCount();
    }

    public class PlaceOrderRequest
    {
        public Guid Id { get; set; }

        public string BaseCurrency { get; set; }

        public string QuoteCurrency { get; set; }

        public Side Side { get; set; }

        public decimal Price { get; set; }

        public decimal Amount { get; set; }
    }
}
