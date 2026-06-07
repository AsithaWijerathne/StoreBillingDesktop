namespace StoreBillingDesktop.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; } // Links back to the Order
    
    // We snapshot the details so price changes don't ruin old receipts!
    public string ProductBarcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    
    public Order Order { get; set; } = null!;
}