using Shared.Models;

namespace Edge.Service.Services;

public interface ICentralApiService
{
    Task<SyncDataDto> RequestSyncAsync(string macAddress);
    Task SendAcknowledgmentAsync(SyncAcknowledgmentDto acknowledgment);
}