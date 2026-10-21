using System.Text.Json;

namespace WebsiteAPI.Entities;

public class Theme
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public JsonDocument Tokens { get; set; } = null!;
    public int? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User? UpdatedByUser { get; set; }
}
