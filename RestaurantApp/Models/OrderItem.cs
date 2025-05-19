using System;
using System.Collections.Generic;

namespace RestaurantApp.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // For display
        public string ProductName { get; set; }
    }
}