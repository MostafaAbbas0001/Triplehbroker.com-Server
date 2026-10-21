using System.Text.Json;

namespace WebsiteAPI.Contracts;

public sealed record ThemeResponse(
    Guid Id,
    string Name,
    bool IsActive,
    JsonDocument Tokens,
    DateTime UpdatedAt);

public sealed class UpdateThemeRequest
{
    public string? Name { get; set; }
    public JsonDocument Tokens { get; set; } = null!;
}
