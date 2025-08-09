using Shared.Models;

namespace Central.Api.Services;

public interface ISnapshotBuilderService
{
    Task<SyncDataDto> BuildSnapshotAsync(string manifestId, DeviceScopeData scopedData, int locationId);
}