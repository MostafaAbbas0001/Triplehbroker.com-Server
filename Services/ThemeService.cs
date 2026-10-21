using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Contracts;
using WebsiteAPI.Data;

namespace WebsiteAPI.Services;

public sealed class ThemeService(ApplicationDbContext context, AuditService audit)
{
    public async Task<ThemeResponse?> GetActiveAsync(CancellationToken cancellationToken)
    {
        var theme = await context.Themes.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.IsActive, cancellationToken);
        return theme is null
            ? null
            : new ThemeResponse(theme.Id, theme.Name, theme.IsActive, theme.Tokens, theme.UpdatedAt);
    }

    public async Task<ThemeResponse?> UpdateAsync(
        Guid id,
        UpdateThemeRequest request,
        int? userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (request.Tokens is null || request.Tokens.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Theme tokens must be a JSON object.");
        if (System.Text.Encoding.UTF8.GetByteCount(request.Tokens.RootElement.GetRawText()) > 256 * 1024)
            throw new ArgumentException("Theme tokens exceed the 256 KB limit.");
        var theme = await context.Themes.FindAsync([id], cancellationToken);
        if (theme is null) return null;
        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name.Length is 0 or > 200) throw new ArgumentException("A valid theme name is required.");
            theme.Name = name;
        }
        theme.Tokens = JsonDocument.Parse(request.Tokens.RootElement.GetRawText());
        theme.UpdatedByUserId = userId;
        theme.UpdatedAt = DateTime.UtcNow;
        audit.Add(userId, "theme.updated", "theme", theme.Id.ToString(), null, ipAddress);
        await context.SaveChangesAsync(cancellationToken);
        return new ThemeResponse(theme.Id, theme.Name, theme.IsActive, theme.Tokens, theme.UpdatedAt);
    }
}
