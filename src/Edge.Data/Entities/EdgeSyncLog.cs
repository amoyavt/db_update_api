namespace Edge.Data.Entities;

public class EdgeSyncLog
{
    public int Id { get; set; }
    public string ManifestId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public string? ErrorText { get; set; }

    public ICollection<EdgeSyncTable> Tables { get; set; } = new List<EdgeSyncTable>();
}