using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using StoreBillingDesktop.Data;
using StoreBillingDesktop.Models;

namespace StoreBillingDesktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<Product> CurrentBill { get; } = new();
 
    [ObservableProperty]
    private string _scanResultMessage = "Ready for next scan...";

    [ObservableProperty]
    private decimal _billTotal = 0.00m;

    // ---Add product to te database---
    [ObservableProperty]
    private string _newBarcode = "";

    [ObservableProperty]
    private string _newName = "";

    [ObservableProperty]
    private string _newPrice = "";

    [ObservableProperty]
    private string _adminMessage = "Enter details to add stock";

    public void ProcessBarcode(string barcode)
    {
        using var db = new AppDbContext();
        var product = db.Products.FirstOrDefault(p => p.Barcode == barcode);

        if (product != null)
        {
            CurrentBill.Add(product);
            BillTotal += product.Price;
            ScanResultMessage = $"✅ Added: {product.Name}";
        }
        else
        {
            ScanResultMessage = $"❌ Item not found: {barcode}";
        }
    }

    //---Add product to te database---
    [RelayCommand]
    private void AddNewProduct()
    {
        //validation
        if (string.IsNullOrWhiteSpace(NewBarcode) || string.IsNullOrWhiteSpace(NewName))
        {
            AdminMessage = "❌ Barcode and Name are required.";
            return;
        }

        if (!decimal.TryParse(NewPrice, out decimal price))
        {
            AdminMessage = "❌ Invalid price. Enter a positive number.";
            return;
        }

        using var db = new AppDbContext();
        // check barcode already eeeeeexists
        if (db.Products.Any(p => p.Barcode == NewBarcode))
        {
            AdminMessage = "❌ Barcode already exists.";
            return;
        }

        var newProduct = new Product
        {
            Barcode = NewBarcode,
            Name = NewName,
            Price = price
        };

        db.Products.Add(newProduct);
        db.SaveChanges();

        AdminMessage = $"✅ Successfully added {NewName}!";
        NewBarcode = "";
        NewName = "";
        NewPrice = "";
    }
}