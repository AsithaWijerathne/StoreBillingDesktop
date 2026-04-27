using Avalonia.Controls;
using Avalonia.Input;
using StoreBillingDesktop.ViewModels;

namespace StoreBillingDesktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void BarcodeScannerInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ProcessScan();
        }
    }

    // NEW: Handles the Enter key press from the Quantity box
    private void BillingQuantityInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ProcessScan();
        }
    }

    // SHARED LOGIC: We pull this into its own method so we don't duplicate code
    private void ProcessScan()
    {
        // Explicitly grab the barcode text box from the UI
        var barcodeBox = this.FindControl<TextBox>("BarcodeScannerInput");
        string scannedCode = barcodeBox?.Text?.Trim() ?? "";

        if (!string.IsNullOrEmpty(scannedCode))
        {
            var viewModel = (MainViewModel)DataContext!;
            viewModel.ProcessBarcode(scannedCode);
            
            if (barcodeBox != null)
            {
                // Clear the barcode box for the next item
                barcodeBox.Text = "";
                
                // Instantly throw the blinking cursor back into the scanner box!
                barcodeBox.Focus(); 
            }
        }
    }
}