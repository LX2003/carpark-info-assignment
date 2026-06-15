using CarparkInfo.Api.Data;
using CarparkInfo.Api.Importing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CarparkInfo.Tests;

public sealed class CsvCarparkImportServiceTests
{
    [Fact]
    public async Task ImportAsync_imports_csv_rows_into_normalized_tables()
    {
        await using var database = await TestDatabase.CreateAsync();
        var csvPath = TestCsv.Write(
            "\"car_park_no\",\"address\",\"x_coord\",\"y_coord\",\"car_park_type\",\"type_of_parking_system\",\"short_term_parking\",\"free_parking\",\"night_parking\",\"car_park_decks\",\"gantry_height\",\"car_park_basement\"",
            "\"ACB\",\"BLK 270/271 ALBERT CENTRE BASEMENT CAR PARK\",\"30314.7936\",\"31490.4942\",\"BASEMENT CAR PARK\",\"ELECTRONIC PARKING\",\"WHOLE DAY\",\"NO\",\"YES\",\"1\",\"1.80\",\"Y\"",
            "\"ACM\",\"BLK 98A ALJUNIED CRESCENT\",\"33758.4143\",\"33695.5198\",\"MULTI-STOREY CAR PARK\",\"ELECTRONIC PARKING\",\"WHOLE DAY\",\"SUN & PH FR 7AM-10.30PM\",\"YES\",\"5\",\"2.10\",\"N\"");

        var service = new CsvCarparkImportService(database.Context);

        var result = await service.ImportAsync(csvPath);

        Assert.Equal(2, result.RowsImported);
        Assert.Equal(2, await database.Context.Carparks.CountAsync());
        Assert.Equal(2, await database.Context.CarparkTypes.CountAsync());
        Assert.Single(await database.Context.ParkingSystems.ToListAsync());
        Assert.Equal(2, await database.Context.FreeParkingRules.CountAsync());
        Assert.True(await database.Context.Carparks.AnyAsync(c => c.CarParkNo == "ACB" && c.NightParking && c.HasBasement));
    }

    [Fact]
    public async Task ImportAsync_rolls_back_entire_file_when_any_row_is_invalid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var csvPath = TestCsv.Write(
            "\"car_park_no\",\"address\",\"x_coord\",\"y_coord\",\"car_park_type\",\"type_of_parking_system\",\"short_term_parking\",\"free_parking\",\"night_parking\",\"car_park_decks\",\"gantry_height\",\"car_park_basement\"",
            "\"ACB\",\"BLK 270/271 ALBERT CENTRE BASEMENT CAR PARK\",\"30314.7936\",\"31490.4942\",\"BASEMENT CAR PARK\",\"ELECTRONIC PARKING\",\"WHOLE DAY\",\"NO\",\"YES\",\"1\",\"1.80\",\"Y\"",
            "\"BAD\",\"BROKEN HEIGHT\",\"30314.7936\",\"31490.4942\",\"BASEMENT CAR PARK\",\"ELECTRONIC PARKING\",\"WHOLE DAY\",\"NO\",\"YES\",\"1\",\"not-a-number\",\"N\"");

        var service = new CsvCarparkImportService(database.Context);

        await Assert.ThrowsAsync<CsvImportException>(() => service.ImportAsync(csvPath));
        Assert.Empty(await database.Context.Carparks.ToListAsync());
        Assert.Empty(await database.Context.CarparkTypes.ToListAsync());
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(SqliteConnection connection, CarparkDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public CarparkDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<CarparkDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new CarparkDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private static class TestCsv
    {
        public static string Write(params string[] lines)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, string.Join(Environment.NewLine, lines));
            return path;
        }
    }
}
