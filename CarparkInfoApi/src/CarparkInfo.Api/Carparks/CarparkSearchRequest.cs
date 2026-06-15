namespace CarparkInfo.Api.Carparks;

public sealed record CarparkSearchRequest(
    bool? HasFreeParking,
    bool? HasNightParking,
    decimal? MinimumVehicleHeight);
