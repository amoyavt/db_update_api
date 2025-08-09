using System.Text;
using System.Text.Json;
using Shared.Models;

namespace Edge.Service.Services;

public class CentralApiService : ICentralApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CentralApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CentralApiService(HttpClient httpClient, ILogger<CentralApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<SyncDataDto> RequestSyncAsync(string macAddress)
    {
        _logger.LogInformation("Requesting sync for MAC {MacAddress}", macAddress);

        var request = new SyncRequestDto(macAddress);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/device-sync/request", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var syncData = JsonSerializer.Deserialize<SyncDataDto>(responseContent, _jsonOptions);

        if (syncData == null)
        {
            throw new InvalidOperationException("Failed to deserialize sync response");
        }

        _logger.LogInformation("Successfully requested sync for MAC {MacAddress}, ManifestId: {ManifestId}",
            macAddress, syncData.Manifest.ManifestId);

        return syncData;
    }

    public async Task SendAcknowledgmentAsync(SyncAcknowledgmentDto acknowledgment)
    {
        _logger.LogInformation("Sending acknowledgment for ManifestId {ManifestId}", acknowledgment.ManifestId);

        var json = JsonSerializer.Serialize(acknowledgment, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/device-sync/ack", content);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Successfully sent acknowledgment for ManifestId {ManifestId}", acknowledgment.ManifestId);
    }
}