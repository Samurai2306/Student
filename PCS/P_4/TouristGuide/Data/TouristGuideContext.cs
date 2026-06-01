using Microsoft.EntityFrameworkCore;
using TouristGuide.Models;

namespace TouristGuide.Data;

public class TouristGuideContext : DbContext
{
    public TouristGuideContext(DbContextOptions<TouristGuideContext> options) : base(options)
    {
    }

    public DbSet<City> Cities => Set<City>();

    public DbSet<Attraction> Attractions => Set<Attraction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(150);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.CoatOfArmsUrl).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Attraction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.OpeningHours).HasMaxLength(200);
            entity.Property(e => e.EntryFee).HasPrecision(10, 2);

            entity.HasOne(a => a.City)
                .WithMany(c => c.Attractions)
                .HasForeignKey(a => a.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
