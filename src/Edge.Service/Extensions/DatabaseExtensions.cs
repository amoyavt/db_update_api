using Microsoft.EntityFrameworkCore;
using Edge.Service.Data;

namespace Edge.Service.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EdgeDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Ensuring edge database is created...");
            await context.Database.EnsureCreatedAsync();

            logger.LogInformation("Edge database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the edge database");
            throw;
        }
    }
}