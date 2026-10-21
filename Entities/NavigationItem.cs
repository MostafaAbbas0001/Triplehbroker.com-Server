using System.Text.Json;

namespace WebsiteAPI.Entities;

public class NavigationItem
{
    public Guid Id { get; set; }
    public Guid MenuId { get; set; }
    public Guid? ParentItemId { get; set; }
    public Guid? PageId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string LinkType { get; set; } = "page";
    public string? Url { get; set; }
    public string? Anchor { get; set; }
    public int SortOrder { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsVisible { get; set; } = true;
    public JsonDocument Style { get; set; } = JsonDocument.Parse("{}");
    public NavigationMenu Menu { get; set; } = null!;
    public NavigationItem? ParentItem { get; set; }
    public ICollection<NavigationItem> Children { get; set; } = new List<NavigationItem>();
    public Page? Page { get; set; }
}
