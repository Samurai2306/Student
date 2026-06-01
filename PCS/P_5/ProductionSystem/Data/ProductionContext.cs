using Microsoft.EntityFrameworkCore;
using ProductionSystem.Models;

namespace ProductionSystem.Data;

public class ProductionContext : DbContext
{
    public ProductionContext(DbContextOptions<ProductionContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<ProductMaterial> ProductMaterials => Set<ProductMaterial>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Specifications).HasMaxLength(4000);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(20);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.MinimalStock).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProductMaterial>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.MaterialId });
            entity.Property(e => e.QuantityNeeded).HasPrecision(18, 2);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.ProductMaterials)
                .HasForeignKey(e => e.ProductId);

            entity.HasOne(e => e.Material)
                .WithMany(m => m.ProductMaterials)
                .HasForeignKey(e => e.MaterialId);
        });

        modelBuilder.Entity<ProductionLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(e => e.CurrentWorkOrder)
                .WithMany()
                .HasForeignKey(e => e.CurrentWorkOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.WorkOrders)
                .HasForeignKey(e => e.ProductId);

            entity.HasOne(e => e.ProductionLine)
                .WithMany(l => l.WorkOrders)
                .HasForeignKey(e => e.ProductionLineId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
