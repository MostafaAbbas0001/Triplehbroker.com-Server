namespace WebsiteAPI.Contracts;

public sealed record UploadResponse(
    Guid Id,
    string FileName,
    string Url,
    string ContentType,
    long Size,
    DateTime UploadedAtUtc,
    string? AltText,
    string? Title);
