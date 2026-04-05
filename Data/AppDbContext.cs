using Microsoft.EntityFrameworkCore;
using StoreBillingDesktop.Models;

namespace StoreBillingDesktop.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=store_billing.db");
    }
}