using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.EntityFrameworkCore;
using StoreBillingDesktop.Data;
using StoreBillingDesktop.Models;
using System.Text.Json;
using System.Collections.Generic;

namespace StoreBillingDesktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<CartItem> CurrentBill { get; } = new();
    public ObservableCollection<Product> InventoryList { get; } = new();
    public ObservableCollection<Order> SalesHistory { get; } = new();
    public ObservableCollection<AlertMessage> AlertsList { get; } = new();
    public ObservableCollection<SuspendedCart> SuspendedBills { get; } = new();

    // --- SECURITY ---
    [ObservableProperty] private bool _isLoggedIn = false;
    [ObservableProperty] private bool _isAdmin = false;
    [ObservableProperty] private string _loginPin = "";
    [ObservableProperty] private string _loginMessage = "Enter your secure PIN to access the terminal.";

    [ObservableProperty] private bool _isHomeViewOpen = true;
    [ObservableProperty] private bool _isReportsViewOpen = false;
    [ObservableProperty] private bool _isSettingsViewOpen = false;

    [ObservableProperty] private string _homeNavColor = "#374151"; // Active by default
    [ObservableProperty] private string _reportsNavColor = "Transparent";
    [ObservableProperty] private string _settingsNavColor = "Transparent";

    // --- GLOBAL SETTINGS ---
    [ObservableProperty] private bool _isLoyaltyEnabled = true;
    [ObservableProperty] private bool _isDiscountEnabled = true;
    [ObservableProperty] private bool _isTaxEnabled = true;

    // --- BILLING / MATH ---
    [ObservableProperty] private string _scanResultMessage = "Ready for next scan...";
    [ObservableProperty] private decimal _subTotal = 0.00m;
    [ObservableProperty] private decimal _discountPercentage = 0.00m;
    [ObservableProperty] private decimal _discountAmount = 0.00m;
    [ObservableProperty] private decimal _taxPercentage = 0.00m;
    [ObservableProperty] private decimal _taxAmount = 0.00m;
    [ObservableProperty] private decimal _billTotal = 0.00m;
    [ObservableProperty] private string _billingQuantity = "1";

    // --- CUSTOMER LOYALTY ---
    [ObservableProperty] private string _customerPhone = "";
    [ObservableProperty] private string _customerDetails = "No customer selected.";
    private Customer? _currentCustomer = null; 

    public string[] UnitTypes { get; } = { "Unit", "Kg", "L" };
    [ObservableProperty] private string _newBaseUnit = "Unit";

    [ObservableProperty] private bool _isQuantityPromptOpen = false;
    [ObservableProperty] private Product? _promptProduct;
    [ObservableProperty] private string _promptInputQuantity = "1";
    [ObservableProperty] private string _promptSelectedUnit = "";
    public ObservableCollection<string> PromptAvailableUnits { get; } = new();

    // --- FORMS & POP-UPS ---
    [ObservableProperty] private string _newBarcode = "";
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newPrice = "";
    [ObservableProperty] private string _newQuantity = "";
    [ObservableProperty] private DateTime? _newExpirationDate; 
    [ObservableProperty] private string _adminMessage = "";
    [ObservableProperty] private string _searchQuery = "";
    
    [ObservableProperty] private bool _isFormOpen = false;
    [ObservableProperty] private bool _isReceiptOpen = false;
    [ObservableProperty] private string _receiptText = "";
    [ObservableProperty] private bool _isPaymentOpen = false;
    [ObservableProperty] private string _amountTenderedInput = "";
    [ObservableProperty] private decimal _changeDue = 0.00m;
    [ObservableProperty] private string _paymentMessage = "Enter cash amount...";
    [ObservableProperty] private bool _isSuspendedBillsOpen = false;

    [ObservableProperty] private decimal _totalRevenue = 0.00m;
    [ObservableProperty] private bool _isAlertsOpen = false;
    [ObservableProperty] private int _alertCount = 0;
    [ObservableProperty] private bool _hasAlerts = false;
    [ObservableProperty] private string _alertButtonColor = "#9CA3AF";

    public MainViewModel()
    {
        LoadSettings();
        LoadParkedBills();
        RefreshInventory();
    }
    
    // --- PERSISTENCE & DATA SAVING ---
    private void LoadSettings()
    {
        if (File.Exists("settings.json"))
        {
            try {
                var json = File.ReadAllText("settings.json");
                var settings = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (settings != null) {
                    if (settings.ContainsKey("Loyalty")) IsLoyaltyEnabled = settings["Loyalty"];
                    if (settings.ContainsKey("Discount")) IsDiscountEnabled = settings["Discount"];
                    if (settings.ContainsKey("Tax")) IsTaxEnabled = settings["Tax"];
                }
            } catch { } // If file is corrupted, it just uses the defaults
        }
    }

    private void SaveSettings()
    {
        var settings = new Dictionary<string, bool> {
            { "Loyalty", IsLoyaltyEnabled },
            { "Discount", IsDiscountEnabled },
            { "Tax", IsTaxEnabled }
        };
        File.WriteAllText("settings.json", JsonSerializer.Serialize(settings));
    }

    private void SaveParkedBills() => File.WriteAllText("parked_bills.json", JsonSerializer.Serialize(SuspendedBills));

    private void LoadParkedBills()
    {
        if (File.Exists("parked_bills.json"))
        {
            try {
                var json = File.ReadAllText("parked_bills.json");
                var loaded = JsonSerializer.Deserialize<ObservableCollection<SuspendedCart>>(json);
                if (loaded != null) foreach (var cart in loaded) SuspendedBills.Add(cart);
            } catch { }
        }
    }

    // --- NEW: SIDEBAR NAVIGATION LOGIC ---
    [RelayCommand]
    public void Navigate(string viewName)
    {
        IsHomeViewOpen = viewName == "Home";
        IsReportsViewOpen = viewName == "Reports";
        IsSettingsViewOpen = viewName == "Settings";

        // NEW: Update the button background colors dynamically!
        HomeNavColor = IsHomeViewOpen ? "#374151" : "Transparent";
        ReportsNavColor = IsReportsViewOpen ? "#374151" : "Transparent";
        SettingsNavColor = IsSettingsViewOpen ? "#374151" : "Transparent";

        // Auto-refresh reports if they click the Reports tab
        if (viewName == "Reports") RefreshReports();
    }

    private void RefreshReports()
    {
        using var db = new AppDbContext();
        var orders = db.Orders.Include(o => o.OrderItems).OrderByDescending(o => o.OrderDate).ToList();
        SalesHistory.Clear(); 
        foreach (var order in orders) SalesHistory.Add(order);
        TotalRevenue = orders.Sum(o => o.TotalAmount);
    }

    // --- SETTINGS TRIGGERS ---
    partial void OnIsLoyaltyEnabledChanged(bool value)
    {
        SaveSettings();
        if (!value) { CustomerPhone = ""; CustomerDetails = "System disabled."; _currentCustomer = null; }
        else { CustomerDetails = "No customer selected."; }
    }
    partial void OnIsDiscountEnabledChanged(bool value) { 
        SaveSettings();
        if(!value) DiscountPercentage = 0; RecalculateTotal(); 
        }
    partial void OnIsTaxEnabledChanged(bool value) { 
        SaveSettings();
        if(!value) TaxPercentage = 0; RecalculateTotal(); 
        }
    partial void OnDiscountPercentageChanged(decimal value) => RecalculateTotal();
    partial void OnTaxPercentageChanged(decimal value) => RecalculateTotal();


    // --- SECURITY ---
    [RelayCommand]
    public void Login()
    {
        if (LoginPin == "1234") { IsAdmin = true; IsLoggedIn = true; LoginMessage = ""; LoginPin = ""; Navigate("Home"); }
        else if (LoginPin == "0000") { IsAdmin = false; IsLoggedIn = true; LoginMessage = ""; LoginPin = ""; Navigate("Home"); }
        else { LoginMessage = "❌ Incorrect PIN. Access Denied."; LoginPin = ""; }
    }

    [RelayCommand]
    public void Logout()
    {
        IsLoggedIn = false; IsAdmin = false; Navigate("Home");
        CurrentBill.Clear(); RecalculateTotal(); LoginMessage = "Enter your secure PIN to access the terminal.";
    }

    // --- DATA MANAGEMENT ---
    [RelayCommand]
    public void BackupDatabase()
    {
        try
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string backupFolder = Path.Combine(desktopPath, "POS_Backups");
            if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);
            string destinationPath = Path.Combine(backupFolder, $"Database_Backup_{DateTime.Now:yyyy-MMM-dd_HH-mm-ss}.db");
            File.Copy("store_billing.db", destinationPath, true);
            ScanResultMessage = $"✅ Database backed up to Desktop/POS_Backups!";
        }
        catch (Exception ex) { ScanResultMessage = $"❌ Backup failed: {ex.Message}"; }
    }

    [RelayCommand]
    public void ExportSalesToCSV()
    {
        try
        {
            using var db = new AppDbContext();
            var orders = db.Orders.OrderByDescending(o => o.OrderDate).ToList();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string exportFolder = Path.Combine(desktopPath, "POS_Reports");
            if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
            string filePath = Path.Combine(exportFolder, $"SalesReport_{DateTime.Now:yyyy-MMM-dd_HH-mm-ss}.csv");
            var sb = new StringBuilder();
            sb.AppendLine("Order ID,Date,Subtotal,Discount,Tax,Grand Total");
            foreach (var order in orders) sb.AppendLine($"{order.Id},{order.OrderDate:yyyy-MM-dd HH:mm},{order.SubTotal},{order.DiscountAmount},{order.TaxAmount},{order.TotalAmount}");
            File.WriteAllText(filePath, sb.ToString());
            ScanResultMessage = $"✅ Sales exported to Desktop/POS_Reports!";
        }
        catch (Exception ex) { ScanResultMessage = $"❌ CSV Export failed: {ex.Message}"; }
    }

    // --- EVERYTHING ELSE (Remains identical to previous steps) ---
    partial void OnSearchQueryChanged(string value) => RefreshInventory();
    private void RefreshInventory()
    {
        using var db = new AppDbContext(); var query = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchQuery)) query = query.Where(p => p.Name.ToLower().Contains(SearchQuery.ToLower()) || p.Barcode.Contains(SearchQuery));
        InventoryList.Clear(); foreach (var p in query.ToList()) InventoryList.Add(p);
        RefreshAlerts();
    }
    private void RefreshAlerts()
    {
        AlertsList.Clear(); var today = DateTime.Now.Date; var thirtyDays = today.AddDays(30);
        foreach (var product in InventoryList)
        {
            if (product.StockQuantity <= 5m) AlertsList.Add(new AlertMessage { Title = "Low Stock", Description = $"{product.Name} is running out!", Icon = "📉" });
            if (product.ExpirationDate.HasValue)
            {
                var expDate = product.ExpirationDate.Value.Date;
                if (expDate <= today) AlertsList.Add(new AlertMessage { Title = "Expired!", Description = $"{product.Name} expired.", Icon = "❌" });
                else if (expDate <= thirtyDays) AlertsList.Add(new AlertMessage { Title = "Expiring Soon", Description = $"{product.Name} expires in {(expDate - today).Days} days.", Icon = "⏳" });
            }
        }
        AlertCount = AlertsList.Count; HasAlerts = AlertCount > 0; AlertButtonColor = HasAlerts ? "#EF4444" : "#9CA3AF"; 
    }

    [RelayCommand] public void OpenAlerts() => IsAlertsOpen = true;
    [RelayCommand] public void CloseAlerts() => IsAlertsOpen = false;

    public void ProcessBarcode(string barcode)
    {
        using var db = new AppDbContext();
        var product = db.Products.FirstOrDefault(p => p.Barcode == barcode);

        if (product == null)
        {
            ScanResultMessage = $"❌ Not found: {barcode}";
            return;
        }

        // Set up the prompt data
        PromptProduct = product;
        PromptInputQuantity = "1";

        // Adapt the dropdown based on the product's Base Unit
        PromptAvailableUnits.Clear();
        if (product.BaseUnit == "Kg") { PromptAvailableUnits.Add("Kg"); PromptAvailableUnits.Add("g"); }
        else if (product.BaseUnit == "L") { PromptAvailableUnits.Add("L"); PromptAvailableUnits.Add("ml"); }
        else { PromptAvailableUnits.Add("Unit"); }

        PromptSelectedUnit = PromptAvailableUnits.First(); // Select the first one by default

        IsQuantityPromptOpen = true; // Show the overlay!
    }
    
    [RelayCommand]
    public void ConfirmQuantity()
    {
        if (PromptProduct == null || !decimal.TryParse(PromptInputQuantity, out decimal inputQty) || inputQty <= 0) return;

        // Calculate the actual base quantity. (e.g. 500g becomes 0.5 Kg)
        decimal actualBaseQuantity = inputQty;
        if (PromptSelectedUnit == "g" || PromptSelectedUnit == "ml")
        {
            actualBaseQuantity = inputQty / 1000m;
        }

        using var db = new AppDbContext();
        var dbProduct = db.Products.Find(PromptProduct.Id);
        
        if (dbProduct != null && dbProduct.StockQuantity >= actualBaseQuantity)
        {
            // Deduct stock from DB
            dbProduct.StockQuantity -= actualBaseQuantity; 
            db.SaveChanges();
            
            // Format the display string (e.g., "500 g" or "2 Kg")
            string displayLabel = $"{inputQty} {PromptSelectedUnit}";

            // Check if item is already in cart
            var existing = CurrentBill.FirstOrDefault(c => c.Item.Barcode == PromptProduct.Barcode);
            if (existing != null) 
            {
                // Note: We standardize the cart to base units behind the scenes to keep math simple
                existing.Quantity += actualBaseQuantity; 
                existing.DisplayUnit = $"{existing.Quantity} {dbProduct.BaseUnit}"; 
            } 
            else 
            {
                CurrentBill.Add(new CartItem(dbProduct, actualBaseQuantity, displayLabel));
            }

            RecalculateTotal(); 
            ScanResultMessage = $"✅ Added: {displayLabel} {dbProduct.Name}"; 
            RefreshInventory(); 
            IsQuantityPromptOpen = false;
        }
        else
        {
            ScanResultMessage = $"⚠️ Not enough stock! Only {dbProduct?.StockQuantity} {dbProduct?.BaseUnit} left.";
            IsQuantityPromptOpen = false;
        }
    }

    [RelayCommand]
    public void CancelQuantityPrompt() => IsQuantityPromptOpen = false;

    private void RecalculateTotal()
    {
        SubTotal = CurrentBill.Sum(item => item.TotalPrice);
        DiscountAmount = IsDiscountEnabled ? SubTotal * (DiscountPercentage / 100m) : 0;
        decimal afterDiscount = SubTotal - DiscountAmount;
        TaxAmount = IsTaxEnabled ? afterDiscount * (TaxPercentage / 100m) : 0;
        BillTotal = afterDiscount + TaxAmount;
        OnAmountTenderedInputChanged(AmountTenderedInput);
    }

    [RelayCommand]
    public void RemoveFromCart(CartItem cartItem)
    {
        using var db = new AppDbContext(); var product = db.Products.FirstOrDefault(p => p.Barcode == cartItem.Item.Barcode);
        if (product != null) { product.StockQuantity += cartItem.Quantity; db.SaveChanges(); }
        CurrentBill.Remove(cartItem); RecalculateTotal(); RefreshInventory();
    }

    [RelayCommand]
    public void SuspendCurrentBill()
    {
        if (!CurrentBill.Any()) return;
        var suspended = new SuspendedCart { TotalAmount = BillTotal };
        foreach (var item in CurrentBill) suspended.Items.Add(item);
        SuspendedBills.Add(suspended); CurrentBill.Clear(); RecalculateTotal(); DiscountPercentage = 0; TaxPercentage = 0;
        ScanResultMessage = $"⏸️ Bill parked (ID: {suspended.Id}).";

        SaveParkedBills();
    }
    [RelayCommand] public void OpenSuspendedBills() => IsSuspendedBillsOpen = true;
    [RelayCommand] public void CloseSuspendedBills() => IsSuspendedBillsOpen = false;
    [RelayCommand]
    public void ResumeBill(SuspendedCart cart)
    {
        if (CurrentBill.Any()) { ScanResultMessage = "⚠️ Finish current bill first!"; IsSuspendedBillsOpen = false; return; }
        foreach (var item in cart.Items) CurrentBill.Add(item);
        SuspendedBills.Remove(cart); RecalculateTotal(); IsSuspendedBillsOpen = false; ScanResultMessage = $"▶️ Resumed Bill {cart.Id}";

        SaveParkedBills();
    }
    [RelayCommand]
    public void DeleteSuspendedBill(SuspendedCart cart)
    {
        using var db = new AppDbContext();
        foreach (var cartItem in cart.Items) { var product = db.Products.FirstOrDefault(p => p.Barcode == cartItem.Item.Barcode); if (product != null) product.StockQuantity += cartItem.Quantity; }
        db.SaveChanges(); SuspendedBills.Remove(cart); RefreshInventory();

        SaveParkedBills();
    }

    [RelayCommand]
    public void SearchCustomer()
    {
        if (string.IsNullOrWhiteSpace(CustomerPhone)) return;
        using var db = new AppDbContext(); var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == CustomerPhone);
        if (customer != null) { _currentCustomer = customer; CustomerDetails = $"✅ Found: {customer.Name} | Points: {customer.LoyaltyPoints}"; }
        else { _currentCustomer = new Customer { PhoneNumber = CustomerPhone }; db.Customers.Add(_currentCustomer); db.SaveChanges(); CustomerDetails = $"✨ New Customer Created | Points: 0"; }
    }

    partial void OnAmountTenderedInputChanged(string value)
    {
        if (decimal.TryParse(value, out decimal tendered)) { ChangeDue = tendered - BillTotal; PaymentMessage = ChangeDue < 0 ? "⚠️ Not enough cash!" : $"✅ Change Due: Rs. {ChangeDue:N2}"; }
        else { ChangeDue = 0; PaymentMessage = "Enter valid amount..."; }
    }

    [RelayCommand]
    public void OpenPayment() { if (!CurrentBill.Any()) { ScanResultMessage = "⚠️ Empty bill!"; return; } AmountTenderedInput = ""; ChangeDue = 0; PaymentMessage = "Enter cash amount..."; CustomerPhone = ""; CustomerDetails = "No customer selected."; _currentCustomer = null; IsPaymentOpen = true; }
    [RelayCommand] public void CancelPayment() => IsPaymentOpen = false;

    [RelayCommand]
    public void FinalizeSale()
    {
        if (!decimal.TryParse(AmountTenderedInput, out decimal tendered) || tendered < BillTotal) { PaymentMessage = "❌ Not enough cash."; return; }
        using var db = new AppDbContext();
        var newOrder = new Order { OrderDate = DateTime.Now, SubTotal = SubTotal, DiscountAmount = DiscountAmount, TaxAmount = TaxAmount, TotalAmount = BillTotal };
        var sb = new StringBuilder();
        sb.AppendLine("       ASITHA'S SUPERMARKET       \n           Sri Lanka              \n----------------------------------\n$" + $"Date: {newOrder.OrderDate:yyyy-MM-dd hh:mm tt}\n----------------------------------");
        foreach (var cartItem in CurrentBill) { newOrder.OrderItems.Add(new OrderItem { ProductBarcode = cartItem.Item.Barcode, ProductName = cartItem.Item.Name, Quantity = cartItem.Quantity, UnitPrice = cartItem.Item.Price, TotalPrice = cartItem.TotalPrice }); sb.AppendLine(cartItem.Item.Name); sb.AppendLine($"  {cartItem.Quantity} x {cartItem.Item.Price:N2}".PadRight(22) + $"Rs. {cartItem.TotalPrice:N2}"); }
        db.Orders.Add(newOrder); 
        int points = 0; if (IsLoyaltyEnabled && _currentCustomer != null) { points = (int)(BillTotal / 100m); var dbC = db.Customers.Find(_currentCustomer.Id); if (dbC != null) dbC.LoyaltyPoints += points; }
        db.SaveChanges();
        sb.AppendLine("----------------------------------"); sb.AppendLine($"SUBTOTAL:".PadRight(22) + $"Rs. {SubTotal:N2}");
        if (DiscountAmount > 0) sb.AppendLine($"DISCOUNT ({DiscountPercentage}%):".PadRight(22) + $"-Rs. {DiscountAmount:N2}");
        if (TaxAmount > 0) sb.AppendLine($"TAX ({TaxPercentage}%):".PadRight(22) + $"+Rs. {TaxAmount:N2}");
        sb.AppendLine("----------------------------------"); sb.AppendLine($"GRAND TOTAL:".PadRight(22) + $"Rs. {BillTotal:N2}"); sb.AppendLine($"CASH TENDERED:".PadRight(22) + $"Rs. {tendered:N2}"); sb.AppendLine($"CHANGE DUE:".PadRight(22) + $"Rs. {ChangeDue:N2}"); sb.AppendLine("----------------------------------"); 
        if (IsLoyaltyEnabled && _currentCustomer != null) { sb.AppendLine($"Loyalty Member: {_currentCustomer.PhoneNumber}\nPoints Earned: {points}\nTotal Points: {_currentCustomer.LoyaltyPoints + points}\n----------------------------------"); }
        sb.AppendLine("     Thank you for shopping!      ");
        ReceiptText = sb.ToString(); IsPaymentOpen = false; IsReceiptOpen = true; CurrentBill.Clear(); RecalculateTotal(); ScanResultMessage = "✅ Sale Complete."; RefreshInventory(); DiscountPercentage = 0; TaxPercentage = 0;
    }

    [RelayCommand] public void CloseReceipt() { IsReceiptOpen = false; ReceiptText = ""; }
    [RelayCommand] public void OpenNewProductForm() { ClearForm(); IsFormOpen = true; }
    [RelayCommand] public void CloseForm() { IsFormOpen = false; ClearForm(); }
    
    [RelayCommand] 
    public void EditProduct(Product product) { 
        NewBarcode = product.Barcode; NewName = product.Name; NewPrice = product.Price.ToString("0.00"); 
        NewQuantity = product.StockQuantity.ToString("0.##"); NewExpirationDate = product.ExpirationDate; 
        NewBaseUnit = product.BaseUnit; 
        IsFormOpen = true; 
    }
    [RelayCommand] 
    public void SaveProduct() { 
        if (string.IsNullOrWhiteSpace(NewBarcode) || string.IsNullOrWhiteSpace(NewName) || !decimal.TryParse(NewPrice, out decimal price) || !decimal.TryParse(NewQuantity, out decimal quantity)) return; 
        using var db = new AppDbContext(); var existing = db.Products.FirstOrDefault(p => p.Barcode == NewBarcode); 
        if (existing != null) { 
            existing.Name = NewName; existing.Price = price; existing.StockQuantity = quantity; 
            existing.ExpirationDate = NewExpirationDate; existing.BaseUnit = NewBaseUnit; // <-- ADD THIS
        } else db.Products.Add(new Product { 
            Barcode = NewBarcode, Name = NewName, Price = price, StockQuantity = quantity, 
            ExpirationDate = NewExpirationDate, BaseUnit = NewBaseUnit // <-- ADD THIS
        }); 
        db.SaveChanges(); RefreshInventory(); IsFormOpen = false; 
    }

    private void ClearForm() { NewBarcode = ""; NewName = ""; NewPrice = ""; NewQuantity = ""; NewExpirationDate = null; NewBaseUnit = "Unit"; }

    [RelayCommand] public void DeleteProduct(Product product) { using var db = new AppDbContext(); db.Products.Remove(product); db.SaveChanges(); RefreshInventory(); }
    [RelayCommand] public void AddDirectlyToCart(Product product) => ProcessBarcode(product.Barcode);

    [RelayCommand]
    public void ExitApplication()
    {
        // SAFETY PROTOCOL: If the app is shut down while items are sitting in the checkout scanner,
        // we MUST return them to the database so the inventory numbers stay perfectly accurate!
        if (CurrentBill.Any())
        {
            using var db = new AppDbContext();
            foreach (var cartItem in CurrentBill)
            {
                var product = db.Products.FirstOrDefault(p => p.Barcode == cartItem.Item.Barcode);
                if (product != null) product.StockQuantity += cartItem.Quantity;
            }
            db.SaveChanges();
        }
        // command to instantly close the entire program
        Environment.Exit(0);
    }
}