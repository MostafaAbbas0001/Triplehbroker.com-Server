using WebsiteAPI.Objects;

namespace WebsiteAPI.Contracts;

public record PageResponse(
    Guid Id,
    string Title,
    string Slug,
    SeoData SeoData,
    DateTime UpdatedAt);
