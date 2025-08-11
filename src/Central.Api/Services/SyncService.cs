using Microsoft.EntityFrameworkCore;
using Central.Api.Data;
using Central.Api.Data.Entities;
using Shared.Models;
using Shared.Infrastructure;

namespace Central.Api.Services;

public class SyncService : ISyncService
{
    private readonly CentralDbContext _context;
    private readonly IDeviceScopeService _deviceScopeService;
    private readonly ISnapshotBuilderService _snapshotBuilderService;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        CentralDbContext context,
        IDeviceScopeService deviceScopeService,
        ISnapshotBuilderService snapshotBuilderService,
        ILogger<SyncService> logger)
    {
        _context = context;
        _deviceScopeService = deviceScopeService;
        _snapshotBuilderService = snapshotBuilderService;
        _logger = logger;
    }

    public async Task<SyncDataDto> ProcessSyncRequestAsync(SyncRequestDto request)
    {
        var manifestId = UlidGenerator.NewUlid();
        _logger.LogInformation("Processing sync request for MAC {Mac} with ManifestId {ManifestId}", request.Mac, manifestId);

        var device = await _context.Devices
            .Include(d => d.Location)
            .ThenInclude(l => l.Company)
            .FirstOrDefaultAsync(d => d.MacAddress == request.Mac);

        if (device == null)
        {
            _logger.LogWarning("Device not found for MAC {Mac}", request.Mac);
            await LogSyncRequest(request.Mac, manifestId, "Failed", "Device not found");
            throw new InvalidOperationException($"Device with MAC {request.Mac} not found");
        }

        try
        {
            var scopedData = await _deviceScopeService.GetScopedDataAsync(device.Id);
            var syncData = await _snapshotBuilderService.BuildSnapshotAsync(manifestId, scopedData, device.LocationId);

            await LogSyncRequest(request.Mac, manifestId, "Success", null);
            await LogSyncManifests(syncData.Manifest, device.LocationId);

            _logger.LogInformation("Successfully built snapshot for MAC {Mac} with {TableCount} tables", 
                request.Mac, syncData.Manifest.Tables.Count);

            return syncData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process sync request for MAC {Mac}", request.Mac);
            await LogSyncRequest(request.Mac, manifestId, "Failed", ex.Message);
            throw;
        }
    }

    public async Task ProcessSyncAcknowledgmentAsync(SyncAcknowledgmentDto acknowledgment)
    {
        _logger.LogInformation("Processing sync acknowledgment for MAC {Mac} and ManifestId {ManifestId}", 
            acknowledgment.Mac, acknowledgment.ManifestId);

        var syncAck = new SyncAcknowledgement
        {
            ManifestId = acknowledgment.ManifestId,
            Mac = acknowledgment.Mac,
            CompletedAt = DateTime.UtcNow,
            Result = acknowledgment.Status,
            DurationMs = acknowledgment.DurationMs,
            DeviceCountsJson = System.Text.Json.JsonSerializer.Serialize(acknowledgment.LocalCounts),
            DeviceHashesJson = System.Text.Json.JsonSerializer.Serialize(acknowledgment.LocalChecksums),
            ErrorText = acknowledgment.Error
        };

        _context.SyncAcknowledgements.Add(syncAck);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully processed sync acknowledgment for MAC {Mac}", acknowledgment.Mac);
    }

    private async Task LogSyncRequest(string mac, string manifestId, string status, string? reason)
    {
        var syncRequest = new SyncRequest
        {
            Mac = mac,
            ManifestId = manifestId,
            RequestedAt = DateTime.UtcNow,
            Status = status,
            Reason = reason
        };

        _context.SyncRequests.Add(syncRequest);
        await _context.SaveChangesAsync();
    }

    private async Task LogSyncManifests(SyncManifestDto manifest, int locationId)
    {
        var manifestEntities = manifest.Tables.Select(table => new SyncManifest
        {
            ManifestId = manifest.ManifestId,
            GeneratedAt = manifest.GeneratedAt,
            TableName = table.Name,
            RowCount = table.RowCount,
            Sha256 = table.Sha256,
            FilterDesc = $"LocationId: {locationId}"
        });

        _context.SyncManifests.AddRange(manifestEntities);
        await _context.SaveChangesAsync();
    }
}