using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using StoreBillingDesktop.Data;
using StoreBillingDesktop.Models;

namespace StoreBillingDesktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<CartItem> CurrentBill { get; } = new();
    public ObservableCollection<Product> InventoryList { get; } = new();

    [ObservableProperty] private string _scanResultMessage = "Ready for next scan...";
    [ObservableProperty] private decimal _billTotal = 0.00m;
    
    // FIXED: Changed to a string to stop Avalonia from crashing when the text box is empty!
    [ObservableProperty] private string _billingQuantity = "1";

    [ObservableProperty] private string _newBarcode = "";
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newPrice = "";
    [ObservableProperty] private string _newQuantity = "";
    [ObservableProperty] private DateTime? _newExpirationDate; 
    [ObservableProperty] private string _adminMessage = "Enter details to add or edit stock.";
    [ObservableProperty] private string _searchQuery = "";

    public MainViewModel()
    {
        RefreshInventory();
    }

    partial void OnSearchQueryChanged(string value) => RefreshInventory();

    private void RefreshInventory()
    {
        using var db = new AppDbContext();
        var query = db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            string lowerSearch = SearchQuery.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(lowerSearch) || p.Barcode.Contains(SearchQuery));
        }

        InventoryList.Clear();
        foreach (var p in query.ToList()) InventoryList.Add(p);
    }

    public void ProcessBarcode(string barcode)
    {
        // FIXED: Safely attempt to parse the string quantity into a decimal
        if (!decimal.TryParse(BillingQuantity, out decimal qty) || qty <= 0)
        {
            ScanResultMessage = "❌ Quantity must be a valid number greater than 0!";
            return;
        }

        using var db = new AppDbContext();
        var product = db.Products.FirstOrDefault(p => p.Barcode == barcode);

        if (product != null)
        {
            if (product.StockQuantity >= qty)
            {
                product.StockQuantity -= qty;
                db.SaveChanges();

                var existingCartItem = CurrentBill.FirstOrDefault(c => c.Item.Barcode == barcode);
                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += qty;
                }
                else
                {
                    CurrentBill.Add(new CartItem(product, qty));
                }

                RecalculateTotal();
                ScanResultMessage = $"✅ Added: {qty}x {product.Name}";
                BillingQuantity = "1"; // Reset the text box back to "1" automatically
                RefreshInventory();
            }
            else
            {
                ScanResultMessage = $"⚠️ Not enough stock! Only {product.StockQuantity} left.";
            }
        }
        else
        {
            ScanResultMessage = $"❌ Item not found: {barcode}";
        }
    }
    
    // --- Add this new command inside your MainViewModel class ---
    [RelayCommand]
    public void RemoveFromCart(CartItem cartItem)
    {
        using var db = new AppDbContext();
        
        // 1. Find the original product in the database
        var product = db.Products.FirstOrDefault(p => p.Barcode == cartItem.Item.Barcode);

        if (product != null)
        {
            // 2. Add the quantity back to the stock!
            product.StockQuantity += cartItem.Quantity;
            db.SaveChanges();
        }

        // 3. Remove it from the UI bill
        CurrentBill.Remove(cartItem);
        
        // 4. Update the totals and lists
        RecalculateTotal();
        RefreshInventory();
        ScanResultMessage = $"🗑️ Removed {cartItem.Item.Name} from bill.";
    }

    private void RecalculateTotal()
    {
        BillTotal = CurrentBill.Sum(item => item.TotalPrice);
    }

    [RelayCommand]
    public void SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(NewBarcode) || string.IsNullOrWhiteSpace(NewName)) return;
        if (!decimal.TryParse(NewPrice, out decimal price)) return;
        if (!decimal.TryParse(NewQuantity, out decimal quantity)) return;

        using var db = new AppDbContext();
        var existingProduct = db.Products.FirstOrDefault(p => p.Barcode == NewBarcode);

        if (existingProduct != null)
        {
            existingProduct.Name = NewName;
            existingProduct.Price = price;
            existingProduct.StockQuantity = quantity;
            existingProduct.ExpirationDate = NewExpirationDate;
            AdminMessage = $"✅ Updated {NewName}!";
        }
        else
        {
            var newProduct = new Product
            {
                Barcode = NewBarcode,
                Name = NewName,
                Price = price,
                StockQuantity = quantity,
                ExpirationDate = NewExpirationDate
            };
            db.Products.Add(newProduct);
            AdminMessage = $"✅ Added {NewName}!";
        }

        db.SaveChanges();
        ClearForm();
        RefreshInventory();
    }

    [RelayCommand]
    public void EditProduct(Product product)
    {
        NewBarcode = product.Barcode;
        NewName = product.Name;
        NewPrice = product.Price.ToString("0.00");
        NewQuantity = product.StockQuantity.ToString("0.##");
        
        NewExpirationDate = product.ExpirationDate;
            
        AdminMessage = "✏️ Editing item. Click Save to update.";
    }

    [RelayCommand]
    public void DeleteProduct(Product product)
    {
        using var db = new AppDbContext();
        db.Products.Remove(product);
        db.SaveChanges();
        AdminMessage = $"🗑️ Deleted {product.Name}.";
        RefreshInventory();
    }

    [RelayCommand]
    public void AddDirectlyToCart(Product product)
    {
        ProcessBarcode(product.Barcode);
    }

    [RelayCommand]
    public void ClearForm()
    {
        NewBarcode = ""; NewName = ""; NewPrice = ""; NewQuantity = ""; 
        NewExpirationDate = null;
        AdminMessage = "Form cleared.";
    }
}