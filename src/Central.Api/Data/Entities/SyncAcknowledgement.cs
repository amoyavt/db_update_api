namespace Central.Api.Data.Entities;

public class SyncAcknowledgement
{
    public string ManifestId { get; set; } = string.Empty;
    public string Mac { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string Result { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public string DeviceCountsJson { get; set; } = string.Empty;
    public string DeviceHashesJson { get; set; } = string.Empty;
    public string? ErrorText { get; set; }
}