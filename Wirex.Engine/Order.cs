using System;

namespace Wirex.Engine
{
    public class Order : IEquatable<Order>
    {
        private readonly decimal _amount;
        private readonly CurrencyPair _currencyPair;
        private readonly decimal _price;
        private readonly Side _side;
        private decimal _remainingAmount;

        public Order(Guid id, CurrencyPair currencyPair, Side side, decimal price, decimal amount)
        {
            Id = id;
            _currencyPair = currencyPair;
            _price = price;
            _side = side;
            _amount = amount;
            _remainingAmount = amount;
        }

        public Guid Id { get; }

        public bool Equals(Order other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id.Equals(other.Id);
        }

        public OrderSnapshot GetSnapshot()
        {
            return new OrderSnapshot
            {
                Id = Id,
                BaseCurrency = _currencyPair.BaseCurrency,
                QuoteCurrency = _currencyPair.QuoteCurrency,
                Side = _side,
                Price = _price,
                Amount = _amount,
                Remaining = _remainingAmount
            };
        }

        public bool IsClosed() => _remainingAmount == 0.0m;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Order) obj);
        }

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString()
            =>
                $"Id: {Id}, CurrencyPair: {_currencyPair}, Price: {_price}, Side: {_side}, Amount: {_amount}, RemainingAmount: {_remainingAmount}";

        public bool TryMatch(Order other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (_side == other._side || !_currencyPair.Equals(other._currencyPair))
                return false;

            switch (_side)
            {
                case Side.Buy when other._price <= _price:
                    Close(other);
                    return true;
                case Side.Sell when other._price > _price:
                    Close(other);
                    return true;
                default:
                    return false;
            }
        }

        private void Close(Order right)
        {
            if (_remainingAmount >= right._remainingAmount)
            {
                _remainingAmount -= right._remainingAmount;
                right._remainingAmount = 0;
            }
            else
            {
                right._remainingAmount -= _remainingAmount;
                _remainingAmount = 0;
            }
        }

        public static Order Restore(OrderSnapshot snapshot)
        {
            var currency = new CurrencyPair(snapshot.BaseCurrency, snapshot.QuoteCurrency);
            return new Order(snapshot.Id, currency, snapshot.Side, snapshot.Price, snapshot.Amount)
            {
                _remainingAmount = snapshot.Remaining
            };
        }
    }
}