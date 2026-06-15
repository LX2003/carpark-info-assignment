namespace CarparkInfo.Api.Importing;

public sealed class CsvImportException : Exception
{
    public CsvImportException(string message)
        : base(message)
    {
    }

    public CsvImportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
