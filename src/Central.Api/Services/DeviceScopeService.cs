using Microsoft.EntityFrameworkCore;
using Central.Api.Data;
using Shared.Models;

namespace Central.Api.Services;

public class DeviceScopeService : IDeviceScopeService
{
    private readonly CentralDbContext _context;
    private readonly ILogger<DeviceScopeService> _logger;

    public DeviceScopeService(CentralDbContext context, ILogger<DeviceScopeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DeviceScopeData> GetScopedDataAsync(int deviceId)
    {
        _logger.LogInformation("Getting scoped data for device {DeviceId}", deviceId);

        var device = await _context.Devices
            .Include(d => d.Location)
            .ThenInclude(l => l.Company)
            .FirstOrDefaultAsync(d => d.Id == deviceId);

        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceId} not found");
        }

        var locationId = device.LocationId;
        var companyId = device.Location.CompanyId;

        var companies = await _context.Companies
            .Where(c => c.Id == companyId)
            .OrderBy(c => c.Id)
            .Select(c => new CompanyDto(c.Id, c.Name, c.CreatedAt))
            .ToListAsync();

        var locations = await _context.Locations
            .Where(l => l.Id == locationId)
            .OrderBy(l => l.Id)
            .Select(l => new LocationDto(l.Id, l.CompanyId, l.Name, l.Address, l.CreatedAt))
            .ToListAsync();

        var groups = await _context.Groups
            .Where(g => g.LocationId == locationId)
            .OrderBy(g => g.Id)
            .Select(g => new GroupDto(g.Id, g.LocationId, g.Name, g.Description, g.CreatedAt))
            .ToListAsync();

        var groupIds = groups.Select(g => g.Id).ToList();
        var users = await _context.Users
            .Where(u => groupIds.Contains(u.GroupId))
            .OrderBy(u => u.Id)
            .Select(u => new UserDto(u.Id, u.GroupId, u.Name, u.Email, u.Role, u.CreatedAt))
            .ToListAsync();

        var areas = await _context.Areas
            .Where(a => a.LocationId == locationId)
            .OrderBy(a => a.Id)
            .Select(a => new AreaDto(a.Id, a.LocationId, a.Name, a.Type, a.CreatedAt))
            .ToListAsync();

        var devices = await _context.Devices
            .Where(d => d.Id == deviceId)
            .OrderBy(d => d.Id)
            .Select(d => new DeviceDto(d.Id, d.LocationId, d.MacAddress, d.Name, d.Model, d.CreatedAt))
            .ToListAsync();

        _logger.LogInformation("Retrieved scoped data for device {DeviceId}: {CompanyCount} companies, {LocationCount} locations, {GroupCount} groups, {UserCount} users, {AreaCount} areas, {DeviceCount} devices",
            deviceId, companies.Count, locations.Count, groups.Count, users.Count, areas.Count, devices.Count);

        return new DeviceScopeData(companies, locations, groups, users, areas, devices);
    }
}