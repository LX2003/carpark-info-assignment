namespace CarparkInfo.Api.Models;

public sealed class ShortTermParkingRule
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public ICollection<Carpark> Carparks { get; set; } = new List<Carpark>();
}
