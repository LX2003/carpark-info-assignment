using CarparkInfo.Api.Carparks;
using CarparkInfo.Api.Data;
using CarparkInfo.Api.Importing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CarparkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CarparkDb") ?? "Data Source=carparks.db"));
builder.Services.AddScoped<ICsvCarparkImportService, CsvCarparkImportService>();
builder.Services.AddScoped<ICarparkService, CarparkService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CarparkDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var importPath = GetImportPath(args);
    if (importPath is not null)
    {
        var importer = scope.ServiceProvider.GetRequiredService<ICsvCarparkImportService>();
        var result = await importer.ImportAsync(importPath);
        Console.WriteLine($"Imported {result.RowsImported} carpark rows.");
        return;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

static string? GetImportPath(string[] args)
{
    var importFlagIndex = Array.IndexOf(args, "--import");
    if (importFlagIndex < 0 || importFlagIndex + 1 >= args.Length)
    {
        return null;
    }

    return args[importFlagIndex + 1];
}

public partial class Program
{
}
