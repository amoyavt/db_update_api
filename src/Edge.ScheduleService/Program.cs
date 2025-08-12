using Edge.Data;
using Edge.ScheduleService.Jobs;
using Edge.ScheduleService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://host.docker.internal:5341")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<EdgeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddQuartz(q =>
{
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 10);
});

builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

builder.Services.AddScoped<ScheduleJob>();
builder.Services.AddHostedService<Edge.ScheduleService.Services.ScheduleService>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database");

builder.Services.AddControllers();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EdgeDbContext>();
    
    await EdgeSeedDataService.SeedSchedulesAsync(context);
}

app.UseHealthChecks("/health");
app.MapControllers();

Log.Information("Edge.ScheduleService starting...");
await app.RunAsync();

Log.CloseAndFlush();
