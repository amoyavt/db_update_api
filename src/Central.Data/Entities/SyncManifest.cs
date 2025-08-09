namespace Central.Data.Entities;

public class SyncManifest
{
    public string ManifestId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string TableName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string FilterDesc { get; set; } = string.Empty;
}