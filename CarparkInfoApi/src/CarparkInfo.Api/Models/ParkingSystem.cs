namespace CarparkInfo.Api.Models;

public sealed class ParkingSystem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Carpark> Carparks { get; set; } = new List<Carpark>();
}
