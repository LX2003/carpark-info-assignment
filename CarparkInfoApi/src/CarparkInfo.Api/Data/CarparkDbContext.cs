using CarparkInfo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarparkInfo.Api.Data;

public sealed class CarparkDbContext : DbContext
{
    public CarparkDbContext(DbContextOptions<CarparkDbContext> options)
        : base(options)
    {
    }

    public DbSet<Carpark> Carparks => Set<Carpark>();
    public DbSet<CarparkType> CarparkTypes => Set<CarparkType>();
    public DbSet<ParkingSystem> ParkingSystems => Set<ParkingSystem>();
    public DbSet<ShortTermParkingRule> ShortTermParkingRules => Set<ShortTermParkingRule>();
    public DbSet<FreeParkingRule> FreeParkingRules => Set<FreeParkingRule>();
    public DbSet<Favourite> Favourites => Set<Favourite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carpark>(entity =>
        {
            entity.HasKey(carpark => carpark.CarParkNo);
            entity.Property(carpark => carpark.CarParkNo).HasMaxLength(20);
            entity.Property(carpark => carpark.Address).HasMaxLength(300).IsRequired();
            entity.Property(carpark => carpark.XCoordinate).HasConversion<double>();
            entity.Property(carpark => carpark.YCoordinate).HasConversion<double>();
            entity.Property(carpark => carpark.GantryHeight).HasConversion<double>();
            entity.HasIndex(carpark => carpark.GantryHeight);
            entity.HasIndex(carpark => carpark.NightParking);
        });

        ConfigureLookup<CarparkType>(modelBuilder, "Name");
        ConfigureLookup<ParkingSystem>(modelBuilder, "Name");
        ConfigureLookup<ShortTermParkingRule>(modelBuilder, "Description");
        ConfigureLookup<FreeParkingRule>(modelBuilder, "Description");

        modelBuilder.Entity<Favourite>(entity =>
        {
            entity.HasKey(favourite => favourite.CarParkNo);
            entity.Property(favourite => favourite.CarParkNo).HasMaxLength(20);
            entity.Property(favourite => favourite.CreatedAt).IsRequired();
            entity.HasOne(favourite => favourite.Carpark)
                .WithOne(carpark => carpark.Favourite)
                .HasForeignKey<Favourite>(favourite => favourite.CarParkNo)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLookup<TEntity>(ModelBuilder modelBuilder, string propertyName)
        where TEntity : class
    {
        modelBuilder.Entity<TEntity>().HasIndex(propertyName).IsUnique();
        modelBuilder.Entity<TEntity>().Property<string>(propertyName).HasMaxLength(200).IsRequired();
    }
}
