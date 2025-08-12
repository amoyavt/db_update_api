using Bogus;
using Edge.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edge.Data;

public static class EdgeSeedDataService
{
    public static async Task SeedSchedulesAsync(EdgeDbContext context)
    {
        if (await context.Schedules.AnyAsync())
        {
            return; // Already seeded
        }

        var areas = await context.Areas.ToListAsync();
        if (!areas.Any())
        {
            throw new InvalidOperationException("Areas must be seeded before schedules");
        }

        var faker = new Faker();
        
        var eventTypes = new[] { "Notification", "Alert", "Maintenance", "Backup", "Report", "Cleanup", "Monitor", "Update" };
        var schedules = new List<Schedule>();

        // Generate 2000 schedules spread across areas
        var scheduleGenerator = new Faker<Schedule>()
            .RuleFor(s => s.AreaId, f => f.PickRandom(areas).Id)
            .RuleFor(s => s.Name, f => $"{f.PickRandom(eventTypes)} - {f.Commerce.Product()}")
            .RuleFor(s => s.Description, f => f.Lorem.Sentence(10))
            .RuleFor(s => s.EventType, f => f.PickRandom(eventTypes))
            .RuleFor(s => s.EventData, f => f.System.CommonFileExt())
            .RuleFor(s => s.IsActive, f => f.Random.Bool(0.9f)) // 90% active
            .RuleFor(s => s.CreatedAt, f => f.Date.Recent(30))
            .RuleFor(s => s.ScheduledTimeUtc, f => {
                // Generate schedules for the next 10 minutes to 2 hours (for quick demo)
                var now = DateTime.UtcNow;
                return f.Date.Between(now.AddMinutes(1), now.AddHours(2));
            });

        schedules = scheduleGenerator.Generate(2000);

        context.Schedules.AddRange(schedules);
        await context.SaveChangesAsync();
        
        Console.WriteLine($"Seeded {schedules.Count} schedules across {areas.Count} areas");
        
        // Log distribution by area
        var distribution = schedules.GroupBy(s => s.AreaId)
            .Select(g => new { AreaId = g.Key, Count = g.Count() })
            .OrderBy(x => x.AreaId);
            
        foreach (var dist in distribution)
        {
            var area = areas.First(a => a.Id == dist.AreaId);
            Console.WriteLine($"  Area '{area.Name}' (ID: {dist.AreaId}): {dist.Count} schedules");
        }
    }
}