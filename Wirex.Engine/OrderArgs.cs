using System;

namespace Wirex.Engine
{
    public class OrderArgs : EventArgs
    {
        public OrderArgs(Order order)
        {
            Order = order;
        }

        public Order Order { get; }
    }
}