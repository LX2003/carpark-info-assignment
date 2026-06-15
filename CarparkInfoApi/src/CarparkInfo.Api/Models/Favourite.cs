namespace CarparkInfo.Api.Models;

public sealed class Favourite
{
    public string CarParkNo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Carpark Carpark { get; set; } = null!;
}
