using Shared.Models;

namespace Edge.DbSync.Services;

public interface ISyncProcessorService
{
    Task<bool> ProcessSyncAsync(SyncDataDto syncData);
}