using Bogus;
using Central.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Central.Api.Data;

public static class SeedDataService
{
    public static async Task SeedAsync(CentralDbContext context)
    {
        if (await context.Companies.AnyAsync())
        {
            return; // Already seeded
        }

        var faker = new Faker();

        // Create companies
        var companies = new List<Company>
        {
            new() { Id = 1, Name = "TechCorp Industries", CreatedAt = DateTime.UtcNow.AddDays(-365) },
            new() { Id = 2, Name = "IoT Solutions Ltd", CreatedAt = DateTime.UtcNow.AddDays(-300) }
        };

        context.Companies.AddRange(companies);
        await context.SaveChangesAsync();

        // Create locations
        var locations = new List<Location>
        {
            new() { Id = 1, CompanyId = 1, Name = "San Francisco Office", Address = "123 Tech Street, San Francisco, CA", CreatedAt = DateTime.UtcNow.AddDays(-360) },
            new() { Id = 2, CompanyId = 1, Name = "New York Warehouse", Address = "456 Industrial Ave, New York, NY", CreatedAt = DateTime.UtcNow.AddDays(-350) },
            new() { Id = 3, CompanyId = 2, Name = "London HQ", Address = "789 Smart Building, London, UK", CreatedAt = DateTime.UtcNow.AddDays(-290) }
        };

        context.Locations.AddRange(locations);
        await context.SaveChangesAsync();

        // Create groups
        var groups = new List<Group>
        {
            new() { Id = 1, LocationId = 1, Name = "Engineering Team", Description = "Software and hardware engineers", CreatedAt = DateTime.UtcNow.AddDays(-355) },
            new() { Id = 2, LocationId = 1, Name = "Operations Team", Description = "DevOps and system administrators", CreatedAt = DateTime.UtcNow.AddDays(-350) },
            new() { Id = 3, LocationId = 2, Name = "Warehouse Staff", Description = "Inventory and logistics team", CreatedAt = DateTime.UtcNow.AddDays(-340) },
            new() { Id = 4, LocationId = 3, Name = "Management", Description = "Executive and management team", CreatedAt = DateTime.UtcNow.AddDays(-285) }
        };

        context.Groups.AddRange(groups);
        await context.SaveChangesAsync();

        // Create users
        var userFaker = new Faker<User>()
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
            .RuleFor(u => u.Role, f => f.PickRandom("Developer", "Admin", "Manager", "Operator", "Analyst"))
            .RuleFor(u => u.CreatedAt, f => f.Date.Between(DateTime.UtcNow.AddDays(-300), DateTime.UtcNow.AddDays(-1)));

        var users = new List<User>();

        // Add users to each group
        foreach (var group in groups)
        {
            var groupUsers = userFaker
                .RuleFor(u => u.GroupId, group.Id)
                .Generate(faker.Random.Int(3, 8));

            users.AddRange(groupUsers);
        }

        // Set sequential IDs
        for (int i = 0; i < users.Count; i++)
        {
            users[i].Id = i + 1;
        }

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // Create areas
        var areaFaker = new Faker<Area>()
            .RuleFor(a => a.Name, f => f.PickRandom("Production Floor", "Storage Area", "Office Space", "Server Room", "Reception", "Conference Room"))
            .RuleFor(a => a.Type, f => f.PickRandom("Manufacturing", "Storage", "Office", "Technical", "Common"))
            .RuleFor(a => a.CreatedAt, f => f.Date.Between(DateTime.UtcNow.AddDays(-300), DateTime.UtcNow.AddDays(-1)));

        var areas = new List<Area>();
        int areaId = 1;

        foreach (var location in locations)
        {
            var locationAreas = areaFaker
                .RuleFor(a => a.LocationId, location.Id)
                .Generate(faker.Random.Int(2, 5));

            foreach (var area in locationAreas)
            {
                area.Id = areaId++;
            }

            areas.AddRange(locationAreas);
        }

        context.Areas.AddRange(areas);
        await context.SaveChangesAsync();

        // Create devices
        var deviceFaker = new Faker<Device>()
            .RuleFor(d => d.Name, f => $"Jetson-{f.Random.AlphaNumeric(6)}")
            .RuleFor(d => d.Model, f => f.PickRandom("Jetson Nano", "Jetson Xavier NX", "Jetson AGX Xavier", "Jetson Orin"))
            .RuleFor(d => d.MacAddress, f => string.Join(":", Enumerable.Range(0, 6).Select(_ => f.Random.Hexadecimal(2).Replace("0x", ""))))
            .RuleFor(d => d.CreatedAt, f => f.Date.Between(DateTime.UtcNow.AddDays(-200), DateTime.UtcNow.AddDays(-1)));

        var devices = new List<Device>();
        int deviceId = 1;

        foreach (var location in locations)
        {
            var locationDevices = deviceFaker
                .RuleFor(d => d.LocationId, location.Id)
                .Generate(faker.Random.Int(1, 3));

            foreach (var device in locationDevices)
            {
                device.Id = deviceId++;
            }

            devices.AddRange(locationDevices);
        }

        // Add the specific test device from the system description
        devices.Add(new Device
        {
            Id = deviceId,
            LocationId = 1, // San Francisco Office
            MacAddress = "48:b0:2d:e9:c3:b7", // From system description
            Name = "TestJetson-POC",
            Model = "Jetson Xavier NX",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // Create schedules
        var scheduleFaker = new Faker<Schedule>()
            .RuleFor(s => s.Name, f => $"{f.PickRandom("Alert", "Backup", "Maintenance", "Report")} - {f.Commerce.Product()}")
            .RuleFor(s => s.Description, f => f.Lorem.Sentence(10))
            .RuleFor(s => s.EventType, f => f.PickRandom("Notification", "Alert", "Maintenance", "Backup", "Report", "Cleanup", "Monitor", "Update"))
            .RuleFor(s => s.EventData, f => f.System.CommonFileExt())
            .RuleFor(s => s.IsActive, f => f.Random.Bool(0.85f))
            .RuleFor(s => s.CreatedAt, f => f.Date.Between(DateTime.UtcNow.AddDays(-100), DateTime.UtcNow.AddDays(-1)))
            .RuleFor(s => s.ScheduledTimeUtc, f => f.Date.Between(DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddHours(48)));

        var schedules = new List<Schedule>();
        int scheduleId = 1;

        foreach (var area in areas)
        {
            var areaSchedules = scheduleFaker
                .RuleFor(s => s.AreaId, area.Id)
                .Generate(faker.Random.Int(8, 15));

            foreach (var schedule in areaSchedules)
            {
                schedule.Id = scheduleId++;
            }

            schedules.AddRange(areaSchedules);
        }

        context.Schedules.AddRange(schedules);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {schedules.Count} schedules across {areas.Count} areas");
    }
}