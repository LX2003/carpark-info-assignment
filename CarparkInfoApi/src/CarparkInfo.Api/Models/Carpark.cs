namespace CarparkInfo.Api.Models;

public sealed class Carpark
{
    public string CarParkNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal XCoordinate { get; set; }
    public decimal YCoordinate { get; set; }
    public int CarparkTypeId { get; set; }
    public CarparkType CarparkType { get; set; } = null!;
    public int ParkingSystemId { get; set; }
    public ParkingSystem ParkingSystem { get; set; } = null!;
    public int ShortTermParkingRuleId { get; set; }
    public ShortTermParkingRule ShortTermParkingRule { get; set; } = null!;
    public int FreeParkingRuleId { get; set; }
    public FreeParkingRule FreeParkingRule { get; set; } = null!;
    public bool NightParking { get; set; }
    public int CarParkDecks { get; set; }
    public decimal GantryHeight { get; set; }
    public bool HasBasement { get; set; }
    public Favourite? Favourite { get; set; }
}
