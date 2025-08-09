namespace Shared.Models;

public record SyncRequestDto(string Mac);

public record SyncManifestDto(
    string ManifestId,
    DateTime GeneratedAt,
    int SchemaVersion,
    List<TableManifestDto> Tables,
    DateTime ExpiresAt,
    Dictionary<string, object> Filters
);

public record TableManifestDto(
    string Name,
    int RowCount,
    string Sha256
);

public record SyncDataDto(
    SyncManifestDto Manifest,
    Dictionary<string, object[]> Data
);

public record SyncAcknowledgmentDto(
    string ManifestId,
    string Mac,
    string Status,
    Dictionary<string, int> LocalCounts,
    Dictionary<string, string> LocalChecksums,
    int DurationMs,
    string? Error = null
);

public record DeviceDto(
    int Id,
    int LocationId,
    string MacAddress,
    string Name,
    string Model,
    DateTime CreatedAt
);

public record CompanyDto(
    int Id,
    string Name,
    DateTime CreatedAt
);

public record LocationDto(
    int Id,
    int CompanyId,
    string Name,
    string Address,
    DateTime CreatedAt
);

public record GroupDto(
    int Id,
    int LocationId,
    string Name,
    string Description,
    DateTime CreatedAt
);

public record UserDto(
    int Id,
    int GroupId,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt
);

public record AreaDto(
    int Id,
    int LocationId,
    string Name,
    string Type,
    DateTime CreatedAt
);

public static class SyncStatus
{
    public const string Success = "Success";
    public const string Failed = "Failed";
}