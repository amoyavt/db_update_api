using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
using Edge.Service.Data;
using Edge.Service.Data.Entities;
using Shared.Models;
using Shared.Infrastructure;

namespace Edge.Service.Services;

public class SyncProcessorService : ISyncProcessorService
{
    private readonly EdgeDbContext _context;
    private readonly ILogger<SyncProcessorService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SyncProcessorService(EdgeDbContext context, ILogger<SyncProcessorService> logger)
    {
        _context = context;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<bool> ProcessSyncAsync(SyncDataDto syncData)
    {
        var manifestId = syncData.Manifest.ManifestId;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting sync processing for ManifestId {ManifestId}", manifestId);

        var syncLog = new EdgeSyncLog
        {
            ManifestId = manifestId,
            StartedAt = DateTime.UtcNow,
            Status = "InProgress"
        };

        _context.EdgeSyncLogs.Add(syncLog);
        await _context.SaveChangesAsync();

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            await TruncateTablesAsync();
            await LoadDataAsync(syncData.Data);

            var verification = await VerifyDataAsync(syncData.Manifest);
            if (!verification.IsValid)
            {
                await transaction.RollbackAsync();
                await UpdateSyncLogAsync(syncLog, "Failed", stopwatch.ElapsedMilliseconds, verification.Error);
                return false;
            }

            await transaction.CommitAsync();
            await UpdateSyncLogAsync(syncLog, "Success", stopwatch.ElapsedMilliseconds, null, verification.TableResults);
            await UpdateSyncStateAsync(manifestId);

            _logger.LogInformation("Successfully processed sync for ManifestId {ManifestId} in {Duration}ms",
                manifestId, stopwatch.ElapsedMilliseconds);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process sync for ManifestId {ManifestId}", manifestId);
            await UpdateSyncLogAsync(syncLog, "Failed", stopwatch.ElapsedMilliseconds, ex.Message);
            return false;
        }
    }

    private async Task TruncateTablesAsync()
    {
        var tables = new[] { "\"Devices\"", "\"Areas\"", "\"Users\"", "\"Groups\"", "\"Locations\"", "\"Companies\"" };

        foreach (var table in tables)
        {
            await _context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {table} RESTART IDENTITY CASCADE");
            _logger.LogDebug("Truncated table {Table}", table);
        }
    }

    private async Task LoadDataAsync(Dictionary<string, object[]> data)
    {
        await LoadCompaniesAsync(data["companies"]);
        await LoadLocationsAsync(data["locations"]);
        await LoadGroupsAsync(data["groups"]);
        await LoadUsersAsync(data["users"]);
        await LoadAreasAsync(data["areas"]);
        await LoadDevicesAsync(data["devices"]);
    }

    private async Task LoadCompaniesAsync(object[] data)
    {
        var companies = JsonSerializer.Deserialize<CompanyDto[]>(JsonSerializer.Serialize(data), _jsonOptions);
        if (companies == null) return;

        var entities = companies.Select(c => new Company
        {
            Id = c.Id,
            Name = c.Name,
            CreatedAt = c.CreatedAt
        });

        _context.Companies.AddRange(entities);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Loaded {Count} companies", companies.Length);
    }

    private async Task LoadLocationsAsync(object[] data)
    {
        var locations = JsonSerializer.Deserialize<LocationDto[]>(JsonSerializer.Serialize(data), _jsonOptions);
        if (locations == null) return;

        var entities = locations.Select(l => new Location
        {
            Id = l.Id,
            CompanyId = l.CompanyId,
            Name = l.Name,
            Address = l.Address,
            CreatedAt = l.CreatedAt
        });

        _context.Locations.AddRange(entities);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Loaded {Count} locations", locations.Length);
    }

    private async Task LoadGroupsAsync(object[] data)
    {
        var groups = JsonSerializer.Deserialize<GroupDto[]>(JsonSerializer.Serialize(data), _jsonOptions);
        if (groups == null) return;

        var entities = groups.Select(g => new Group
        {
            Id = g.Id,
            LocationId = g.LocationId,
            Name = g.Name,
            Description = g.Description,
            CreatedAt = g.CreatedAt
        });

        _context.Groups.AddRange(entities);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Loaded {Count} groups", groups.Length);
    }

    private async Task LoadUsersAsync(object[] data)
    {
        var users = JsonSerializer.Deserialize<UserDto[]>(JsonSerializer.Serialize(data), _jsonOptions);
        if (users == null) return;

        var entities = users.Select(u => new User
        {
            Id = u.Id,
            GroupId = u.GroupId,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        });

        _context.Users.AddRange(entities);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Loaded {Count} users", users.Length);
    }

    private async Task LoadAreasAsync(object[] data)
    {
        var areas = JsonSerializer.Deserialize<AreaDto[]>(JsonSerializer.Serialize(data), _jsonOptions);
        if (areas == null) return;

        var entities = areas.Select(a => new Area
        {
            Id = a.Id,
            LocationId = a.LocationId,
            Name = a.Name,
            Type = a.Type,
            CreatedAt = a.CreatedAt
        });

        _context.Areas.AddRange(entities);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Loaded {Count} areas", areas.Length);
    }

    private async Task LoadDevicesAsync(object[] data)
    {
        var devices = JsonSerializer.Deserialize<DeviceDto[]>(JsonSerializer.Serialize(data), _jsonOptions);
        if (devices == null) return;

        var entities = devices.Select(d => new Device
        {
            Id = d.Id,
            LocationId = d.LocationId,
            MacAddress = d.MacAddress,
            Name = d.Name,
            Model = d.Model,
            CreatedAt = d.CreatedAt
        });

        _context.Devices.AddRange(entities);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Loaded {Count} devices", devices.Length);
    }

    private async Task<VerificationResult> VerifyDataAsync(SyncManifestDto manifest)
    {
        var tableResults = new List<EdgeSyncTable>();
        var errors = new List<string>();

        foreach (var tableManifest in manifest.Tables)
        {
            try
            {
                var actualCount = await GetTableCountAsync(tableManifest.Name);
                var actualData = await GetTableDataAsync(tableManifest.Name);
                var actualHash = HashHelper.ComputeSha256Hash(actualData);

                var syncTable = new EdgeSyncTable
                {
                    TableName = tableManifest.Name,
                    RowCount = actualCount,
                    Sha256 = actualHash
                };

                tableResults.Add(syncTable);

                if (actualCount != tableManifest.RowCount)
                {
                    errors.Add($"Row count mismatch for {tableManifest.Name}: expected {tableManifest.RowCount}, got {actualCount}");
                }

                if (actualHash != tableManifest.Sha256)
                {
                    errors.Add($"Hash mismatch for {tableManifest.Name}: expected {tableManifest.Sha256}, got {actualHash}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Verification failed for {tableManifest.Name}: {ex.Message}");
            }
        }

        return new VerificationResult
        {
            IsValid = !errors.Any(),
            Error = errors.Any() ? string.Join("; ", errors) : null,
            TableResults = tableResults
        };
    }

    private async Task<int> GetTableCountAsync(string tableName)
    {
        return tableName switch
        {
            "companies" => await _context.Companies.CountAsync(),
            "locations" => await _context.Locations.CountAsync(),
            "groups" => await _context.Groups.CountAsync(),
            "users" => await _context.Users.CountAsync(),
            "areas" => await _context.Areas.CountAsync(),
            "devices" => await _context.Devices.CountAsync(),
            _ => 0
        };
    }

    private async Task<object[]> GetTableDataAsync(string tableName)
    {
        return tableName switch
        {
            "companies" => await _context.Companies
                .OrderBy(c => c.Id)
                .Select(c => new CompanyDto(c.Id, c.Name, c.CreatedAt))
                .ToArrayAsync(),
            "locations" => await _context.Locations
                .OrderBy(l => l.Id)
                .Select(l => new LocationDto(l.Id, l.CompanyId, l.Name, l.Address, l.CreatedAt))
                .ToArrayAsync(),
            "groups" => await _context.Groups
                .OrderBy(g => g.Id)
                .Select(g => new GroupDto(g.Id, g.LocationId, g.Name, g.Description, g.CreatedAt))
                .ToArrayAsync(),
            "users" => await _context.Users
                .OrderBy(u => u.Id)
                .Select(u => new UserDto(u.Id, u.GroupId, u.Name, u.Email, u.Role, u.CreatedAt))
                .ToArrayAsync(),
            "areas" => await _context.Areas
                .OrderBy(a => a.Id)
                .Select(a => new AreaDto(a.Id, a.LocationId, a.Name, a.Type, a.CreatedAt))
                .ToArrayAsync(),
            "devices" => await _context.Devices
                .OrderBy(d => d.Id)
                .Select(d => new DeviceDto(d.Id, d.LocationId, d.MacAddress, d.Name, d.Model, d.CreatedAt))
                .ToArrayAsync(),
            _ => Array.Empty<object>()
        };
    }

    private async Task UpdateSyncLogAsync(EdgeSyncLog syncLog, string status, long durationMs, string? error, List<EdgeSyncTable>? tables = null)
    {
        syncLog.Status = status;
        syncLog.CompletedAt = DateTime.UtcNow;
        syncLog.DurationMs = (int)durationMs;
        syncLog.ErrorText = error;

        if (tables != null)
        {
            foreach (var table in tables)
            {
                table.EdgeSyncLogId = syncLog.Id;
            }
            _context.EdgeSyncTables.AddRange(tables);
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpdateSyncStateAsync(string manifestId)
    {
        var state = await _context.EdgeSyncStates.FirstOrDefaultAsync(s => s.Key == "last_manifest_id");
        if (state == null)
        {
            state = new EdgeSyncState { Key = "last_manifest_id" };
            _context.EdgeSyncStates.Add(state);
        }

        state.Value = manifestId;
        state.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private class VerificationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public List<EdgeSyncTable> TableResults { get; set; } = new();
    }
}