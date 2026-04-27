using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreBillingDesktop.Models;

// We inherit from ObservableObject so the UI listens to changes
public partial class CartItem : ObservableObject
{
    public Product Item { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))] // Magically updates the total price text!
    private decimal _quantity;

    // A read-only property that calculates the cost on the fly
    public decimal TotalPrice => Item.Price * Quantity;

    public CartItem(Product item, decimal quantity)
    {
        Item = item;
        Quantity = quantity;
    }
}