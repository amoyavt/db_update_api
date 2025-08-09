namespace Central.Data.Entities;

public class Group
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Location Location { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
}