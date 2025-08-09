using Shared.Models;

namespace Central.Api.Services;

public interface ISyncService
{
    Task<SyncDataDto> ProcessSyncRequestAsync(SyncRequestDto request);
    Task ProcessSyncAcknowledgmentAsync(SyncAcknowledgmentDto acknowledgment);
}