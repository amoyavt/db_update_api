namespace Edge.Data.Entities;

public class Area
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}