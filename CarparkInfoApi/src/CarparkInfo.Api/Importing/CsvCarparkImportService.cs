using System.Globalization;
using System.Text;
using CarparkInfo.Api.Data;
using CarparkInfo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarparkInfo.Api.Importing;

public sealed class CsvCarparkImportService : ICsvCarparkImportService
{
    private static readonly string[] ExpectedHeaders =
    {
        "car_park_no",
        "address",
        "x_coord",
        "y_coord",
        "car_park_type",
        "type_of_parking_system",
        "short_term_parking",
        "free_parking",
        "night_parking",
        "car_park_decks",
        "gantry_height",
        "car_park_basement"
    };

    private readonly CarparkDbContext _context;

    public CsvCarparkImportService(CarparkDbContext context)
    {
        _context = context;
    }

    public async Task<CsvImportResult> ImportAsync(string csvPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(csvPath))
        {
            throw new CsvImportException($"CSV file was not found: {csvPath}");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var rows = ParseCsv(csvPath).ToList();
            var imported = 0;

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UpsertCarparkAsync(row, cancellationToken);
                imported++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CsvImportResult(imported);
        }
        catch (CsvImportException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new CsvImportException("CSV import failed. The entire file was rolled back.", exception);
        }
    }

    private async Task UpsertCarparkAsync(CarparkCsvRow row, CancellationToken cancellationToken)
    {
        var type = await GetOrCreateAsync(_context.CarparkTypes, row.CarparkType, name => new CarparkType { Name = name }, cancellationToken);
        var system = await GetOrCreateAsync(_context.ParkingSystems, row.ParkingSystem, name => new ParkingSystem { Name = name }, cancellationToken);
        var shortTerm = await GetOrCreateAsync(_context.ShortTermParkingRules, row.ShortTermParking, description => new ShortTermParkingRule { Description = description }, cancellationToken);
        var freeParking = await GetOrCreateAsync(_context.FreeParkingRules, row.FreeParking, description => new FreeParkingRule { Description = description }, cancellationToken);

        var existing = await _context.Carparks.FindAsync(new object[] { row.CarParkNo }, cancellationToken);
        if (existing is null)
        {
            existing = new Carpark { CarParkNo = row.CarParkNo };
            _context.Carparks.Add(existing);
        }

        existing.Address = row.Address;
        existing.XCoordinate = row.XCoordinate;
        existing.YCoordinate = row.YCoordinate;
        existing.CarparkType = type;
        existing.ParkingSystem = system;
        existing.ShortTermParkingRule = shortTerm;
        existing.FreeParkingRule = freeParking;
        existing.NightParking = row.NightParking;
        existing.CarParkDecks = row.CarParkDecks;
        existing.GantryHeight = row.GantryHeight;
        existing.HasBasement = row.HasBasement;
    }

    private static async Task<TEntity> GetOrCreateAsync<TEntity>(
        DbSet<TEntity> dbSet,
        string value,
        Func<string, TEntity> create,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var propertyName = typeof(TEntity) == typeof(CarparkType) || typeof(TEntity) == typeof(ParkingSystem)
            ? "Name"
            : "Description";

        var tracked = dbSet.Local.FirstOrDefault(entity => GetStringProperty(entity, propertyName) == value);
        if (tracked is not null)
        {
            return tracked;
        }

        var existing = await dbSet.FirstOrDefaultAsync(
            entity => EF.Property<string>(entity, propertyName) == value,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = create(value);
        dbSet.Add(created);
        return created;
    }

    private static string? GetStringProperty<TEntity>(TEntity entity, string propertyName)
    {
        return typeof(TEntity).GetProperty(propertyName)?.GetValue(entity) as string;
    }

    private static IEnumerable<CarparkCsvRow> ParseCsv(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        var header = reader.ReadLine();
        if (header is null)
        {
            throw new CsvImportException("CSV file is empty.");
        }

        var headers = SplitCsvLine(header);
        if (!headers.SequenceEqual(ExpectedHeaders, StringComparer.OrdinalIgnoreCase))
        {
            throw new CsvImportException("CSV header does not match the expected HDB carpark format.");
        }

        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            if (fields.Count != ExpectedHeaders.Length)
            {
                throw new CsvImportException($"Line {lineNumber} has {fields.Count} columns; expected {ExpectedHeaders.Length}.");
            }

            yield return ParseRow(fields, lineNumber);
        }
    }

    private static CarparkCsvRow ParseRow(IReadOnlyList<string> fields, int lineNumber)
    {
        try
        {
            return new CarparkCsvRow(
                Required(fields[0], "car_park_no", lineNumber),
                Required(fields[1], "address", lineNumber),
                Decimal(fields[2], "x_coord", lineNumber),
                Decimal(fields[3], "y_coord", lineNumber),
                Required(fields[4], "car_park_type", lineNumber),
                Required(fields[5], "type_of_parking_system", lineNumber),
                Required(fields[6], "short_term_parking", lineNumber),
                Required(fields[7], "free_parking", lineNumber),
                YesNo(fields[8], "night_parking", lineNumber),
                Int(fields[9], "car_park_decks", lineNumber),
                Decimal(fields[10], "gantry_height", lineNumber),
                YN(fields[11], "car_park_basement", lineNumber));
        }
        catch (CsvImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CsvImportException($"Line {lineNumber} could not be parsed.", exception);
        }
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }

    private static string Required(string value, string columnName, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CsvImportException($"Line {lineNumber}: {columnName} is required.");
        }

        return value.Trim();
    }

    private static decimal Decimal(string value, string columnName, int lineNumber)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new CsvImportException($"Line {lineNumber}: {columnName} must be a decimal number.");
    }

    private static int Int(string value, string columnName, int lineNumber)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new CsvImportException($"Line {lineNumber}: {columnName} must be an integer.");
    }

    private static bool YesNo(string value, string columnName, int lineNumber)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "YES" => true,
            "NO" => false,
            _ => throw new CsvImportException($"Line {lineNumber}: {columnName} must be YES or NO.")
        };
    }

    private static bool YN(string value, string columnName, int lineNumber)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "Y" => true,
            "N" => false,
            _ => throw new CsvImportException($"Line {lineNumber}: {columnName} must be Y or N.")
        };
    }

    private sealed record CarparkCsvRow(
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
}
