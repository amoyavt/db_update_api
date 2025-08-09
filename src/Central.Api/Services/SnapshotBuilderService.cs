using Shared.Models;
using Shared.Infrastructure;

namespace Central.Api.Services;

public class SnapshotBuilderService : ISnapshotBuilderService
{
    private readonly ILogger<SnapshotBuilderService> _logger;

    public SnapshotBuilderService(ILogger<SnapshotBuilderService> logger)
    {
        _logger = logger;
    }

    public async Task<SyncDataDto> BuildSnapshotAsync(string manifestId, DeviceScopeData scopedData, int locationId)
    {
        _logger.LogInformation("Building snapshot for ManifestId {ManifestId}", manifestId);

        var data = new Dictionary<string, object[]>
        {
            ["companies"] = scopedData.Companies.ToArray(),
            ["locations"] = scopedData.Locations.ToArray(),
            ["groups"] = scopedData.Groups.ToArray(),
            ["users"] = scopedData.Users.ToArray(),
            ["areas"] = scopedData.Areas.ToArray(),
            ["devices"] = scopedData.Devices.ToArray()
        };

        var tables = new List<TableManifestDto>();

        foreach (var (tableName, tableData) in data)
        {
            var hash = HashHelper.ComputeSha256Hash(tableData);
            tables.Add(new TableManifestDto(tableName, tableData.Length, hash));
        }

        var manifest = new SyncManifestDto(
            ManifestId: manifestId,
            GeneratedAt: DateTime.UtcNow,
            SchemaVersion: 1,
            Tables: tables,
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            Filters: new Dictionary<string, object> { ["locationId"] = locationId }
        );

        _logger.LogInformation("Built snapshot for ManifestId {ManifestId} with {TableCount} tables and {TotalRows} total rows",
            manifestId, tables.Count, tables.Sum(t => t.RowCount));

        return new SyncDataDto(manifest, data);
    }
}