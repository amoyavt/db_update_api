using Shared.Models;

namespace Central.Api.Services;

public interface IDeviceScopeService
{
    Task<DeviceScopeData> GetScopedDataAsync(int deviceId);
}

public record DeviceScopeData(
    List<CompanyDto> Companies,
    List<LocationDto> Locations,
    List<GroupDto> Groups,
    List<UserDto> Users,
    List<AreaDto> Areas,
    List<DeviceDto> Devices,
    List<ScheduleDto> Schedules
);