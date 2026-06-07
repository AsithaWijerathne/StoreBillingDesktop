using Avalonia.Controls;
using Avalonia.Input; // Required for keyboard events
using StoreBillingDesktop.ViewModels; // Required to talk to the Brain

namespace StoreBillingDesktop.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void BarcodeScannerInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var viewModel = (MainViewModel)DataContext!;
                viewModel.ProcessBarcode(textBox.Text);
                textBox.Text = ""; // Clear the scanner input
            }
        }
    }

    private void BillingQuantityInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Focus back to the barcode scanner
            var scanner = this.FindControl<TextBox>("BarcodeScannerInput");
            scanner?.Focus();
        }
    }
}