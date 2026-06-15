using CarparkInfo.Api.Data;
using CarparkInfo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarparkInfo.Api.Carparks;

public sealed class CarparkService : ICarparkService
{
    private readonly CarparkDbContext _context;

    public CarparkService(CarparkDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CarparkDto>> SearchAsync(CarparkSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = IncludedCarparks();

        if (request.HasFreeParking is true)
        {
            query = query.Where(carpark => carpark.FreeParkingRule.Description != "NO");
        }
        else if (request.HasFreeParking is false)
        {
            query = query.Where(carpark => carpark.FreeParkingRule.Description == "NO");
        }

        if (request.HasNightParking is not null)
        {
            query = query.Where(carpark => carpark.NightParking == request.HasNightParking.Value);
        }

        if (request.MinimumVehicleHeight is not null)
        {
            query = query.Where(carpark => carpark.GantryHeight >= request.MinimumVehicleHeight.Value);
        }

        return await query
            .OrderBy(carpark => carpark.CarParkNo)
            .Select(carpark => ToDto(carpark))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CarparkDto>> GetFavouritesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Favourites
            .Include(favourite => favourite.Carpark)
            .ThenInclude(carpark => carpark.CarparkType)
            .Include(favourite => favourite.Carpark)
            .ThenInclude(carpark => carpark.ParkingSystem)
            .Include(favourite => favourite.Carpark)
            .ThenInclude(carpark => carpark.ShortTermParkingRule)
            .Include(favourite => favourite.Carpark)
            .ThenInclude(carpark => carpark.FreeParkingRule)
            .OrderBy(favourite => favourite.CarParkNo)
            .Select(favourite => ToDto(favourite.Carpark))
            .ToListAsync(cancellationToken);
    }

    public async Task AddFavouriteAsync(string carParkNo, CancellationToken cancellationToken = default)
    {
        var normalizedCarParkNo = carParkNo.Trim().ToUpperInvariant();
        var exists = await _context.Carparks.AnyAsync(carpark => carpark.CarParkNo == normalizedCarParkNo, cancellationToken);
        if (!exists)
        {
            throw new CarparkNotFoundException(normalizedCarParkNo);
        }

        var alreadyFavourite = await _context.Favourites.AnyAsync(favourite => favourite.CarParkNo == normalizedCarParkNo, cancellationToken);
        if (alreadyFavourite)
        {
            return;
        }

        _context.Favourites.Add(new Favourite
        {
            CarParkNo = normalizedCarParkNo,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveFavouriteAsync(string carParkNo, CancellationToken cancellationToken = default)
    {
        var normalizedCarParkNo = carParkNo.Trim().ToUpperInvariant();
        var favourite = await _context.Favourites.FindAsync(new object[] { normalizedCarParkNo }, cancellationToken);
        if (favourite is null)
        {
            return false;
        }

        _context.Favourites.Remove(favourite);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Carpark> IncludedCarparks()
    {
        return _context.Carparks
            .Include(carpark => carpark.CarparkType)
            .Include(carpark => carpark.ParkingSystem)
            .Include(carpark => carpark.ShortTermParkingRule)
            .Include(carpark => carpark.FreeParkingRule);
    }

    private static CarparkDto ToDto(Carpark carpark)
    {
        return new CarparkDto(
            carpark.CarParkNo,
            carpark.Address,
            carpark.XCoordinate,
            carpark.YCoordinate,
            carpark.CarparkType.Name,
            carpark.ParkingSystem.Name,
            carpark.ShortTermParkingRule.Description,
            carpark.FreeParkingRule.Description,
            carpark.NightParking,
            carpark.CarParkDecks,
            carpark.GantryHeight,
            carpark.HasBasement);
    }
}
