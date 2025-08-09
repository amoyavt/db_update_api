using Microsoft.Extensions.Options;
using Edge.Service.Services;
using Edge.Service.Configuration;
using Shared.Models;

namespace Edge.Service.Workers;

public class SyncWorker : BackgroundService
{
    private readonly ICentralApiService _centralApiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<DeviceConfig> _deviceConfig;
    private readonly IOptionsMonitor<SyncConfig> _syncConfig;
    private readonly ILogger<SyncWorker> _logger;

    public SyncWorker(
        ICentralApiService centralApiService,
        IServiceProvider serviceProvider,
        IOptionsMonitor<DeviceConfig> deviceConfig,
        IOptionsMonitor<SyncConfig> syncConfig,
        ILogger<SyncWorker> logger)
    {
        _centralApiService = centralApiService;
        _serviceProvider = serviceProvider;
        _deviceConfig = deviceConfig;
        _syncConfig = syncConfig;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var macAddress = _deviceConfig.CurrentValue.MacAddress;
                if (string.IsNullOrEmpty(macAddress))
                {
                    _logger.LogWarning("MAC address not configured, skipping sync");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                await PerformSyncAsync(macAddress);

                var intervalMinutes = _syncConfig.CurrentValue.IntervalMinutes;
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sync worker");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Sync Worker stopped");
    }

    private async Task PerformSyncAsync(string macAddress)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starting sync for MAC {MacAddress}", macAddress);

        try
        {
            var syncData = await _centralApiService.RequestSyncAsync(macAddress);
            
            bool success;
            using (var scope = _serviceProvider.CreateScope())
            {
                var syncProcessorService = scope.ServiceProvider.GetRequiredService<ISyncProcessorService>();
                success = await syncProcessorService.ProcessSyncAsync(syncData);
            }

            var acknowledgment = new SyncAcknowledgmentDto(
                ManifestId: syncData.Manifest.ManifestId,
                Mac: macAddress,
                Status: success ? SyncStatus.Success : SyncStatus.Failed,
                LocalCounts: GetLocalCounts(syncData.Manifest),
                LocalChecksums: GetLocalChecksums(syncData.Manifest),
                DurationMs: (int)stopwatch.ElapsedMilliseconds,
                Error: success ? null : "Sync processing failed"
            );

            await _centralApiService.SendAcknowledgmentAsync(acknowledgment);

            _logger.LogInformation("Completed sync for MAC {MacAddress} in {Duration}ms with status {Status}",
                macAddress, stopwatch.ElapsedMilliseconds, acknowledgment.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform sync for MAC {MacAddress}", macAddress);
        }
    }

    private static Dictionary<string, int> GetLocalCounts(SyncManifestDto manifest)
    {
        return manifest.Tables.ToDictionary(t => t.Name, t => t.RowCount);
    }

    private static Dictionary<string, string> GetLocalChecksums(SyncManifestDto manifest)
    {
        return manifest.Tables.ToDictionary(t => t.Name, t => t.Sha256);
    }
}