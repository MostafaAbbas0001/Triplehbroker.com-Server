using System.Text.Json;

namespace WebsiteAPI.Entities;

public class SiteSetting
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public JsonDocument Value { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
