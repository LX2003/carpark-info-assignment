namespace CarparkInfo.Api.Carparks;

public sealed class CarparkNotFoundException : Exception
{
    public CarparkNotFoundException(string carParkNo)
        : base($"Carpark '{carParkNo}' was not found.")
    {
        CarParkNo = carParkNo;
    }

    public string CarParkNo { get; }
}
