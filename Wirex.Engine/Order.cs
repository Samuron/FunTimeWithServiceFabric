using System;

namespace Wirex.Engine
{
    public class OrderSnapshot
    {
        public Guid Id { get; set; }

        public string BaseCurrency { get; set; }

        public string QuoteCurrency { get; set; }

        public Side Side { get; set; }

        public decimal Price { get; set; }

        public decimal Amount { get; set; }

        public decimal Remaining { get; set; }
    }

    public class Order : IEquatable<Order>
    {
        public Order(Guid id, CurrencyPair currencyPair, Side side, decimal price, decimal amount)
        {
            Id = id;
            CurrencyPair = currencyPair;
            Price = price;
            Side = side;
            Amount = amount;
            RemainingAmount = amount;
        }

        public Guid Id { get; }

        public CurrencyPair CurrencyPair { get; }

        public Side Side { get; }

        public decimal Price { get; }

        public decimal Amount { get; }

        public decimal RemainingAmount { get; private set; }

        public OrderSnapshot GetSnapshot()
        {
            return new OrderSnapshot
            {
                Id = Id,
                BaseCurrency = CurrencyPair.BaseCurrency,
                QuoteCurrency = CurrencyPair.QuoteCurrency,
                Side = Side,
                Price = Price,
                Amount = Amount,
                Remaining = RemainingAmount
            };
        }

        public bool IsClosed() => RemainingAmount == 0.0m;

        public bool Equals(Order other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Order)obj);
        }

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString()
            => $"Id: {Id}, CurrencyPair: {CurrencyPair}, Price: {Price}, Side: {Side}, Amount: {Amount}, RemainingAmount: {RemainingAmount}";

        public bool TryMatch(Order other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (Side == other.Side || !CurrencyPair.Equals(other.CurrencyPair))
                return false;

            switch (Side)
            {
                case Side.Buy when other.Price <= Price:
                    Close(other);
                    return true;
                case Side.Sell when other.Price > Price:
                    Close(other);
                    return true;
                default:
                    return false;
            }
        }

        private void Close(Order right)
        {
            if (RemainingAmount >= right.RemainingAmount)
            {
                RemainingAmount -= right.RemainingAmount;
                right.RemainingAmount = 0;
            }
            else
            {
                right.RemainingAmount -= RemainingAmount;
                RemainingAmount = 0;
            }
        }

        public static Order Restore(OrderSnapshot snapshot)
        {
            var currency = new CurrencyPair(snapshot.BaseCurrency, snapshot.QuoteCurrency);
            return new Order(snapshot.Id, currency, snapshot.Side, snapshot.Price, snapshot.Amount)
            {
                RemainingAmount = snapshot.Remaining
            };
        }
    }
}