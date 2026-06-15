namespace CarparkInfo.Api.Models;

public sealed class CarparkType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Carpark> Carparks { get; set; } = new List<Carpark>();
}
