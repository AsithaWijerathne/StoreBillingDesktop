using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreBillingDesktop.Models;

// We inherit from ObservableObject so the UI listens to changes
public partial class CartItem : ObservableObject
{
    public Product Item { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))] // updates the total price text!
    private decimal _quantity;

    public decimal TotalPrice => Item.Price * Quantity;

    public string DisplayUnit { get; set; } = "";

    public CartItem() { }

    public CartItem(Product item, decimal quantity, string displayUnit)
    {
        Item = item;
        Quantity = quantity;
        DisplayUnit = displayUnit;
    }
}