using System.Text.Json;

namespace WebsiteAPI.Contracts;

public sealed record NavigationMenuResponse(
    Guid Id,
    string Name,
    string Key,
    string Locale,
    IReadOnlyList<NavigationItemResponse> Items,
    DateTime UpdatedAt);

public sealed record NavigationItemResponse(
    Guid Id,
    string Label,
    string LinkType,
    Guid? PageId,
    string? Url,
    string? Href,
    string? Anchor,
    int SortOrder,
    bool OpenInNewTab,
    bool IsVisible,
    JsonDocument Style,
    IReadOnlyList<NavigationItemResponse> Children);

public sealed class SaveNavigationMenuRequest
{
    public string Name { get; set; } = string.Empty;
    public List<SaveNavigationItemRequest> Items { get; set; } = [];
}

public sealed class SaveNavigationItemRequest
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string LinkType { get; set; } = "page";
    public Guid? PageId { get; set; }
    public string? Url { get; set; }
    public string? Anchor { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsVisible { get; set; } = true;
    public JsonDocument? Style { get; set; }
    public List<SaveNavigationItemRequest> Children { get; set; } = [];
}
