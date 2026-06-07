using System;
using System.Collections.ObjectModel;

namespace StoreBillingDesktop.Models;

public class SuspendedCart
{
    // Generates a random 5-character ID (like "A4F2B")
    public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
    public DateTime TimeSuspended { get; set; } = DateTime.Now;
    
    // Holds the items and the total at the time it was suspended
    public ObservableCollection<CartItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}