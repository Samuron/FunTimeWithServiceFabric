﻿using System;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;

namespace Wirex.Engine
{
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
            return Equals((Order) obj);
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
    }
}