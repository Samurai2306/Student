using InternetShop.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace InternetShop.Server.Data;

public class ShopDbContext : DbContext
{
    public ShopDbContext(DbContextOptions<ShopDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
}
