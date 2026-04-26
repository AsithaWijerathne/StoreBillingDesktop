using Avalonia;
using System;
using System.Linq;
using StoreBillingDesktop.Data;
using StoreBillingDesktop.Models;

namespace StoreBillingDesktop;

class Program
{
    
    [STAThread]
    public static void Main(string[] args)
    {
        SeedDatabase();
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void SeedDatabase()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        if (!db.Products.Any())
        {
            Console.WriteLine("Database is empty. Inserting dummy data...");

            var dummyProducts = new[]
            {
                new Product { Barcode = "84123", Name = "Samba Rice 1kg", Price = 260.00m },
                new Product { Barcode = "84124", Name = "Munchee Super Cream Cracker", Price = 150.00m },
                new Product { Barcode = "84125", Name = "Anchor Milk Powder 400g", Price = 1150.00m },
                new Product { Barcode = "84126", Name = "Dilmah Tea 200g", Price = 420.00m }
            };

            db.Products.AddRange(dummyProducts);
            db.SaveChanges();

            Console.WriteLine("Dummy data inserted successfully.");
        }
        else
        {
            Console.WriteLine("Database already contains data. Skipping seeding.");
        }
    }
}
