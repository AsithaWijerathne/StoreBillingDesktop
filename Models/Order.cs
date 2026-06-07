using System;
using System.Collections.Generic;

namespace StoreBillingDesktop.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    
    public decimal TotalAmount { get; set; } // The final Grand Total
    
    public List<OrderItem> OrderItems { get; set; } = new();
}