namespace CarparkInfo.Api.Importing;

public interface ICsvCarparkImportService
{
    Task<CsvImportResult> ImportAsync(string csvPath, CancellationToken cancellationToken = default);
}
