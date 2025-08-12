using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Edge.DbSync.Services;
using Edge.DbSync.Configuration;

namespace Edge.DbSync.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly ICentralApiService _centralApiService;
    private readonly ISyncProcessorService _syncProcessorService;
    private readonly IOptionsSnapshot<DeviceConfig> _deviceConfig;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ICentralApiService centralApiService,
        ISyncProcessorService syncProcessorService,
        IOptionsSnapshot<DeviceConfig> deviceConfig,
        ILogger<SyncController> logger)
    {
        _centralApiService = centralApiService;
        _syncProcessorService = syncProcessorService;
        _deviceConfig = deviceConfig;
        _logger = logger;
    }

    [HttpPost("now")]
    public async Task<ActionResult> TriggerSyncNow()
    {
        _logger.LogInformation("Manual sync triggered");

        var macAddress = _deviceConfig.Value.MacAddress;
        if (string.IsNullOrEmpty(macAddress))
        {
            _logger.LogWarning("MAC address not configured");
            return BadRequest("MAC address not configured");
        }

        try
        {
            var syncData = await _centralApiService.RequestSyncAsync(macAddress);
            var success = await _syncProcessorService.ProcessSyncAsync(syncData);

            if (success)
            {
                _logger.LogInformation("Manual sync completed successfully for MAC {MacAddress}", macAddress);
                return Ok(new { status = "success", manifestId = syncData.Manifest.ManifestId });
            }
            else
            {
                _logger.LogWarning("Manual sync failed for MAC {MacAddress}", macAddress);
                return StatusCode(500, new { status = "failed", error = "Sync processing failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual sync failed for MAC {MacAddress}", macAddress);
            return StatusCode(500, new { status = "failed", error = ex.Message });
        }
    }
}