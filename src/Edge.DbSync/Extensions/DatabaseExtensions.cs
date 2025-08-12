using Microsoft.EntityFrameworkCore;
using Edge.Data;

namespace Edge.DbSync.Extensions;

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

            logger.LogInformation("Edge database creation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the edge database");
            throw;
        }
    }
}