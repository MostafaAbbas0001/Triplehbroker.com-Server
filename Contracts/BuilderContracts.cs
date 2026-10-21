using System.Text.Json;
using WebsiteAPI.Objects;

namespace WebsiteAPI.Contracts;

public sealed record BuilderPageSummary(
    Guid Id,
    Guid? PageGroupId,
    string Title,
    string Slug,
    string Locale,
    string Template,
    bool IsHomepage,
    bool IsSystemPage,
    DateTime UpdatedAt);

public sealed record BuilderPageResponse(
    Guid Id,
    Guid? PageGroupId,
    string Title,
    string Slug,
    string Locale,
    string Template,
    bool IsHomepage,
    bool IsSystemPage,
    SeoData SeoData,
    JsonDocument Document,
    DateTime UpdatedAt);

public sealed record SolutionPageSummary(
    Guid Id,
    string Title,
    string Slug,
    string Locale,
    string Summary,
    string? ImageUrl,
    int Position);

public sealed class CreateBuilderPageRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
    public string Template { get; set; } = "builder";
    public Guid? PageGroupId { get; set; }
    public bool IsHomepage { get; set; }
    public SeoData SeoData { get; set; } = new();
    public JsonDocument? Document { get; set; }
}

public sealed class UpdateBuilderPageRequest
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Template { get; set; }
    public bool? IsHomepage { get; set; }
    public SeoData? SeoData { get; set; }
}

public sealed class SavePageDocumentRequest
{
    public JsonDocument Document { get; set; } = null!;
}

public sealed class DuplicatePageRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Locale { get; set; }
    public bool KeepTranslationGroup { get; set; }
}
