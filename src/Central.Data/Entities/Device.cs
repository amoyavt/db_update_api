namespace Central.Data.Entities;

public class Device
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string MacAddress { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Location Location { get; set; } = null!;
}