using System;

namespace Wirex.Engine
{
    public class CurrencyPair : IEquatable<CurrencyPair>
    {
        public CurrencyPair(string baseCurrency, string quoteCurrency)
        {
            BaseCurrency = baseCurrency;
            QuoteCurrency = quoteCurrency;
        }

        public string BaseCurrency { get; }

        public string QuoteCurrency { get; }

        public bool Equals(CurrencyPair other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(BaseCurrency, other.BaseCurrency) && string.Equals(QuoteCurrency, other.QuoteCurrency);
        }

        public override string ToString() => $"{BaseCurrency}/,{QuoteCurrency}";

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((CurrencyPair) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((BaseCurrency?.GetHashCode() ?? 0) * 397) ^ (QuoteCurrency?.GetHashCode() ?? 0);
            }
        }
    }
}