using Microsoft.EntityFrameworkCore;
using Edge.Data;
using Edge.DbSync.Services;
using Edge.DbSync.Workers;
using Edge.DbSync.Configuration;
using Edge.DbSync.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq("http://host.docker.internal:5341")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<EdgeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<DeviceConfig>(
    builder.Configuration.GetSection(DeviceConfig.SectionName));

builder.Services.Configure<SyncConfig>(
    builder.Configuration.GetSection(SyncConfig.SectionName));

builder.Services.Configure<CentralApiConfig>(
    builder.Configuration.GetSection(CentralApiConfig.SectionName));

builder.Services.AddHttpClient<ICentralApiService, CentralApiService>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CentralApiConfig>>();
    client.BaseAddress = new Uri(config.Value.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<ISyncProcessorService, SyncProcessorService>();

builder.Services.AddHostedService<SyncWorker>();

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
    Log.Information("Starting Edge DbSync Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Edge DbSync Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}