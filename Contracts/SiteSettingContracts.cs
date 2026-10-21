using System.Text.Json;

namespace WebsiteAPI.Contracts;

public record SiteSettingResponse(
    Guid Id,
    string Key,
    JsonDocument Value,
    DateTime UpdatedAt);

public class SiteSettingPatchRequest
{
    public JsonDocument? Value { get; set; }
}
