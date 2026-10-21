namespace WebsiteAPI.Entities;

public class PageGroup
{
    public Guid Id { get; set; }
    public string InternalName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Page> Pages { get; set; } = new List<Page>();
}
