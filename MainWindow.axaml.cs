using Avalonia.Controls;
using Avalonia.Input;
using StoreBillingDesktop.ViewModels; // Add this to see the ViewModel

namespace StoreBillingDesktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 🤝 THE HANDSHAKE: Tell the window to use MainViewModel for its data
        DataContext = new MainViewModel();
    }

    private void BarcodeScannerInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var textBox = (TextBox)sender!;
            string scannedCode = textBox.Text?.Trim() ?? "";

            if (!string.IsNullOrEmpty(scannedCode))
            {
                // Grab the ViewModel and tell it to do the heavy lifting
                var viewModel = (MainViewModel)DataContext!;
                viewModel.ProcessBarcode(scannedCode);
                
                // Clear the box for the next item
                textBox.Text = "";
            }
        }
    }
}