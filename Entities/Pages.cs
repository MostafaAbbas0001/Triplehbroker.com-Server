using System.Text.Json;
using WebsiteAPI.Objects;

namespace WebsiteAPI.Entities;

public class Page
{
    public Guid Id { get; set; }
    public Guid? PageGroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
    public string Template { get; set; } = "builder";
    public bool IsHomepage { get; set; }
    public bool IsSystemPage { get; set; }
    public JsonDocument? BuilderDocument { get; set; }
    public SeoData SeoData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public PageGroup? PageGroup { get; set; }
}
