using Shared.Models;

namespace Edge.Service.Services;

public interface ISyncProcessorService
{
    Task<bool> ProcessSyncAsync(SyncDataDto syncData);
}