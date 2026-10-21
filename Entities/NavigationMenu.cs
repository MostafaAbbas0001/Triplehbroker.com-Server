namespace WebsiteAPI.Entities;

public class NavigationMenu
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<NavigationItem> Items { get; set; } = new List<NavigationItem>();
}
