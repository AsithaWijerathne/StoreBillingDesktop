using System.Dynamic;
using Avalonia.Controls;

namespace StoreBillingDesktop.Models;

public class AlertMessage
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "⚠️";
}