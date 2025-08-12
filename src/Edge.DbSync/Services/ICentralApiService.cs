using Shared.Models;

namespace Edge.DbSync.Services;

public interface ICentralApiService
{
    Task<SyncDataDto> RequestSyncAsync(string macAddress);
    Task SendAcknowledgmentAsync(SyncAcknowledgmentDto acknowledgment);
}