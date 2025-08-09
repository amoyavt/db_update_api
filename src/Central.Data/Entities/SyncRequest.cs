namespace Central.Data.Entities;

public class SyncRequest
{
    public int Id { get; set; }
    public string Mac { get; set; } = string.Empty;
    public string ManifestId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}