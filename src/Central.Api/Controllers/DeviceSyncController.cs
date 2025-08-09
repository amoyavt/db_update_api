using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Central.Api.Services;
using Shared.Models;

namespace Central.Api.Controllers;

[ApiController]
[Route("api/device-sync")]
public class DeviceSyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly IValidator<SyncRequestDto> _requestValidator;
    private readonly IValidator<SyncAcknowledgmentDto> _ackValidator;
    private readonly ILogger<DeviceSyncController> _logger;

    public DeviceSyncController(
        ISyncService syncService,
        IValidator<SyncRequestDto> requestValidator,
        IValidator<SyncAcknowledgmentDto> ackValidator,
        ILogger<DeviceSyncController> logger)
    {
        _syncService = syncService;
        _requestValidator = requestValidator;
        _ackValidator = ackValidator;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<ActionResult<SyncDataDto>> RequestSync([FromBody] SyncRequestDto request)
    {
        _logger.LogInformation("Received sync request for MAC {Mac}", request.Mac);

        var validationResult = await _requestValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid sync request for MAC {Mac}: {Errors}", 
                request.Mac, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(validationResult.Errors);
        }

        try
        {
            var syncData = await _syncService.ProcessSyncRequestAsync(request);
            _logger.LogInformation("Successfully processed sync request for MAC {Mac}", request.Mac);
            return Ok(syncData);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid sync request for MAC {Mac}", request.Mac);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sync request for MAC {Mac}", request.Mac);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("ack")]
    public async Task<ActionResult> AcknowledgeSync([FromBody] SyncAcknowledgmentDto acknowledgment)
    {
        _logger.LogInformation("Received sync acknowledgment for MAC {Mac} and ManifestId {ManifestId}", 
            acknowledgment.Mac, acknowledgment.ManifestId);

        var validationResult = await _ackValidator.ValidateAsync(acknowledgment);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid sync acknowledgment for MAC {Mac}: {Errors}", 
                acknowledgment.Mac, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(validationResult.Errors);
        }

        try
        {
            await _syncService.ProcessSyncAcknowledgmentAsync(acknowledgment);
            _logger.LogInformation("Successfully processed sync acknowledgment for MAC {Mac}", acknowledgment.Mac);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sync acknowledgment for MAC {Mac}", acknowledgment.Mac);
            return StatusCode(500, "Internal server error");
        }
    }
}