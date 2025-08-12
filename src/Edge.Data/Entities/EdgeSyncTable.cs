namespace Edge.Data.Entities;

public class EdgeSyncTable
{
    public int Id { get; set; }
    public int EdgeSyncLogId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string Sha256 { get; set; } = string.Empty;

    public EdgeSyncLog EdgeSyncLog { get; set; } = null!;
}