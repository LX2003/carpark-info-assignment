using CarparkInfo.Api.Carparks;
using CarparkInfo.Api.Data;
using CarparkInfo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarparkInfo.Tests;

public sealed class CarparkServiceTests
{
    [Fact]
    public async Task SearchAsync_filters_by_free_parking_night_parking_and_minimum_height()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new CarparkService(context);

        var matches = await service.SearchAsync(new CarparkSearchRequest(
            HasFreeParking: true,
            HasNightParking: true,
            MinimumVehicleHeight: 2.0m));

        var match = Assert.Single(matches);
        Assert.Equal("ACM", match.CarParkNo);
    }

    [Fact]
    public async Task AddFavouriteAsync_adds_global_favourite_once()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var service = new CarparkService(context);

        await service.AddFavouriteAsync("ACM");
        await service.AddFavouriteAsync("ACM");

        var favourites = await service.GetFavouritesAsync();
        var favourite = Assert.Single(favourites);
        Assert.Equal("ACM", favourite.CarParkNo);
    }

    [Fact]
    public async Task AddFavouriteAsync_rejects_unknown_carpark()
    {
        await using var context = CreateContext();
        var service = new CarparkService(context);

        await Assert.ThrowsAsync<CarparkNotFoundException>(() => service.AddFavouriteAsync("MISSING"));
    }

    private static CarparkDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CarparkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CarparkDbContext(options);
    }

    private static async Task SeedAsync(CarparkDbContext context)
    {
        var type = new CarparkType { Name = "MULTI-STOREY CAR PARK" };
        var system = new ParkingSystem { Name = "ELECTRONIC PARKING" };
        var shortTerm = new ShortTermParkingRule { Description = "WHOLE DAY" };
        var free = new FreeParkingRule { Description = "SUN & PH FR 7AM-10.30PM" };
        var noFree = new FreeParkingRule { Description = "NO" };

        context.Carparks.AddRange(
            new Carpark
            {
                CarParkNo = "ACM",
                Address = "BLK 98A ALJUNIED CRESCENT",
                XCoordinate = 33758.4143m,
                YCoordinate = 33695.5198m,
                CarparkType = type,
                ParkingSystem = system,
                ShortTermParkingRule = shortTerm,
                FreeParkingRule = free,
                NightParking = true,
                CarParkDecks = 5,
                GantryHeight = 2.10m,
                HasBasement = false
            },
            new Carpark
            {
                CarParkNo = "ACB",
                Address = "BLK 270/271 ALBERT CENTRE BASEMENT CAR PARK",
                XCoordinate = 30314.7936m,
                YCoordinate = 31490.4942m,
                CarparkType = type,
                ParkingSystem = system,
                ShortTermParkingRule = shortTerm,
                FreeParkingRule = noFree,
                NightParking = true,
                CarParkDecks = 1,
                GantryHeight = 1.80m,
                HasBasement = true
            });

        await context.SaveChangesAsync();
    }
}
