using System;

namespace Wirex.Engine
{
    public class OrderSnapshot : IEquatable<OrderSnapshot>
    {
        public Guid Id { get; set; }

        public string BaseCurrency { get; set; }

        public string QuoteCurrency { get; set; }

        public Side Side { get; set; }

        public decimal Price { get; set; }

        public decimal Amount { get; set; }

        public decimal Remaining { get; set; }

        public bool Equals(OrderSnapshot other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id.Equals(other.Id)
                   && string.Equals(BaseCurrency, other.BaseCurrency)
                   && string.Equals(QuoteCurrency, other.QuoteCurrency) 
                   && Side == other.Side 
                   && Price == other.Price
                   && Amount == other.Amount && Remaining == other.Remaining;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((OrderSnapshot) obj);
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}