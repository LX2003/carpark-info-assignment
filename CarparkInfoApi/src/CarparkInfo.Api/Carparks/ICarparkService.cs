namespace CarparkInfo.Api.Carparks;

public interface ICarparkService
{
    Task<IReadOnlyList<CarparkDto>> SearchAsync(CarparkSearchRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarparkDto>> GetFavouritesAsync(CancellationToken cancellationToken = default);
    Task AddFavouriteAsync(string carParkNo, CancellationToken cancellationToken = default);
    Task<bool> RemoveFavouriteAsync(string carParkNo, CancellationToken cancellationToken = default);
}
