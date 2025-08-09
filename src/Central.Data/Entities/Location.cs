namespace Central.Data.Entities;

public class Location
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<Area> Areas { get; set; } = new List<Area>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}