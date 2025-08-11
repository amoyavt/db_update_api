using Microsoft.EntityFrameworkCore;
using Central.Api.Data;
using Central.Api.Services;
using Central.Api.Extensions;
using FluentValidation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<CentralDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IDeviceScopeService, DeviceScopeService>();
builder.Services.AddScoped<ISnapshotBuilderService, SnapshotBuilderService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database");

builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseHealthChecks("/health");
app.UseHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                data = e.Value.Data
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();

await app.InitializeDatabaseAsync();

try
{
    Log.Information("Starting Central API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Central API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}