namespace CarparkInfo.Api.Carparks;

public sealed record CarparkDto(
    string CarParkNo,
    string Address,
    decimal XCoordinate,
    decimal YCoordinate,
    string CarparkType,
    string ParkingSystem,
    string ShortTermParking,
    string FreeParking,
    bool NightParking,
    int CarParkDecks,
    decimal GantryHeight,
    bool HasBasement);
