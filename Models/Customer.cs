namespace StoreBillingDesktop.Models;

public class Customer
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Name { get; set; } = "Valued Customer";
    public int LoyaltyPoints { get; set; } = 0;
}