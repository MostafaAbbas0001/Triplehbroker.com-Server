using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Contracts;
using WebsiteAPI.Data;
using WebsiteAPI.Entities;

namespace WebsiteAPI.Services;

public sealed class BuilderPageService(ApplicationDbContext context, AuditService audit)
{
    public Task<List<BuilderPageSummary>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Pages.AsNoTracking()
            .OrderBy(page => page.Locale)
            .ThenBy(page => page.IsHomepage ? 0 : 1)
            .ThenBy(page => page.Title)
            .Select(page => new BuilderPageSummary(
                page.Id, page.PageGroupId, page.Title, page.Slug, page.Locale, page.Template,
                page.IsHomepage, page.IsSystemPage, page.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<BuilderPageResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var page = await context.Pages.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return page is null ? null : ToResponse(page);
    }

    public async Task<List<SolutionPageSummary>> GetSolutionSummariesAsync(
        string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var pages = await context.Pages.AsNoTracking()
            .Where(page => page.Locale == normalizedLocale && page.Template == "solution-detail")
            .OrderBy(page => page.Title)
            .ToListAsync(cancellationToken);
        return pages.Select(page =>
        {
            var root = page.BuilderDocument?.RootElement;
            var position = 999;
            var displayTitle = page.Title;
            if (root is { } document &&
                document.TryGetProperty("settings", out var settings) &&
                settings.TryGetProperty("catalogPosition", out var savedPosition) &&
                savedPosition.TryGetInt32(out var parsedPosition))
                position = parsedPosition;
            if (root is { } pageDocument &&
                pageDocument.TryGetProperty("children", out var children))
            {
                foreach (var node in children.EnumerateArray())
                {
                    if (!node.TryGetProperty("settings", out var nodeSettings) ||
                        !nodeSettings.TryGetProperty("sectionType", out var sectionType) ||
                        sectionType.GetString() != "solution-introduction" ||
                        !node.TryGetProperty("content", out var content) ||
                        !content.TryGetProperty("heading", out var heading)) continue;
                    displayTitle = heading.GetString() ?? page.Title;
                    break;
                }
            }
            return new SolutionPageSummary(
                page.Id,
                displayTitle,
                page.Slug,
                page.Locale,
                page.SeoData.Description ?? string.Empty,
                page.SeoData.OpenGraph.ImageUrl,
                position);
        }).OrderBy(page => page.Position).ThenBy(page => page.Title).ToList();
    }

    public async Task<BuilderPageResponse?> GetByPathAsync(
        string locale, string? path, CancellationToken cancellationToken)
    {
        var slug = NormalizeSlug(locale, path, string.IsNullOrWhiteSpace(path));
        var page = await context.Pages.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Slug == slug && candidate.Locale == locale,
                cancellationToken);
        return page is null ? null : ToResponse(page);
    }

    public async Task<BuilderPageResponse> CreateAsync(
        CreateBuilderPageRequest request, int? userId, string? ipAddress,
        CancellationToken cancellationToken)
    {
        var title = RequireText(request.Title, "A page title is required.", 300);
        var locale = NormalizeLocale(request.Locale);
        var slug = NormalizeSlug(locale, request.Slug, request.IsHomepage);
        var template = RequireText(request.Template, "A page template is required.", 100);
        await EnsureSlugAvailableAsync(slug, null, cancellationToken);
        if (request.IsHomepage && await context.Pages.AnyAsync(
                page => page.Locale == locale && page.IsHomepage, cancellationToken))
            throw new InvalidOperationException($"The {locale} homepage already exists.");

        var document = request.Document ?? BuilderDocumentValidator.Empty(template);
        var validationError = BuilderDocumentValidator.Validate(document);
        if (validationError is not null) throw new ArgumentException(validationError);

        PageGroup group;
        if (request.PageGroupId is not null)
        {
            group = await context.PageGroups.FindAsync([request.PageGroupId.Value], cancellationToken)
                ?? throw new ArgumentException("The translation group does not exist.");
            if (await context.Pages.AnyAsync(
                    page => page.PageGroupId == group.Id && page.Locale == locale, cancellationToken))
                throw new InvalidOperationException("That translation group already has this language.");
        }
        else
        {
            group = new PageGroup
            {
                Id = Guid.NewGuid(),
                InternalName = await UniqueGroupNameAsync(title, cancellationToken),
                CreatedAt = DateTime.UtcNow
            };
            context.PageGroups.Add(group);
        }

        var now = DateTime.UtcNow;
        var page = new Page
        {
            Id = Guid.NewGuid(),
            PageGroupId = group.Id,
            Title = title,
            Slug = slug,
            Locale = locale,
            Template = template,
            IsHomepage = request.IsHomepage,
            IsSystemPage = false,
            SeoData = request.SeoData,
            BuilderDocument = BuilderDocumentValidator.Clone(document),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Pages.Add(page);
        audit.Add(userId, "page.created", "page", page.Id.ToString(), new { page.Title, page.Slug }, ipAddress);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(page);
    }

    public async Task<BuilderPageResponse?> UpdateMetadataAsync(
        Guid id, UpdateBuilderPageRequest request, int? userId, string? ipAddress,
        CancellationToken cancellationToken)
    {
        var page = await context.Pages.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (page is null) return null;

        var oldSlug = page.Slug;
        if (request.Title is not null) page.Title = RequireText(request.Title, "Title cannot be empty.", 300);
        if (request.Template is not null)
            page.Template = RequireText(request.Template, "Template cannot be empty.", 100);
        if (request.IsHomepage is not null && request.IsHomepage != page.IsHomepage)
        {
            if (request.IsHomepage.Value && await context.Pages.AnyAsync(
                    candidate => candidate.Locale == page.Locale && candidate.IsHomepage && candidate.Id != page.Id,
                    cancellationToken))
                throw new InvalidOperationException($"The {page.Locale} homepage already exists.");
            page.IsHomepage = request.IsHomepage.Value;
        }
        if (request.Slug is not null || request.IsHomepage is not null)
        {
            var slug = NormalizeSlug(page.Locale, request.Slug ?? page.Slug, page.IsHomepage);
            await EnsureSlugAvailableAsync(slug, page.Id, cancellationToken);
            page.Slug = slug;
        }
        if (request.SeoData is not null) page.SeoData = request.SeoData;

        page.UpdatedAt = DateTime.UtcNow;
        if (!string.Equals(oldSlug, page.Slug, StringComparison.Ordinal))
        {
            var redirect = await context.Redirects.SingleOrDefaultAsync(
                candidate => candidate.OldPath == $"/{oldSlug}", cancellationToken);
            if (redirect is null)
            {
                redirect = new Redirect
                {
                    Id = Guid.NewGuid(),
                    OldPath = $"/{oldSlug}",
                    CreatedAt = DateTime.UtcNow
                };
                context.Redirects.Add(redirect);
            }
            redirect.NewPath = $"/{page.Slug}";
            redirect.StatusCode = 301;
            redirect.IsActive = true;
        }
        audit.Add(userId, "page.metadata.updated", "page", page.Id.ToString(), new { oldSlug, page.Slug }, ipAddress);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(page);
    }

    public async Task<BuilderPageResponse?> SaveDocumentAsync(
        Guid id, JsonDocument document, int? userId, string? ipAddress,
        CancellationToken cancellationToken)
    {
        var validationError = BuilderDocumentValidator.Validate(document);
        if (validationError is not null) throw new ArgumentException(validationError);
        var page = await context.Pages.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (page is null) return null;
        page.BuilderDocument = BuilderDocumentValidator.Clone(document);
        page.UpdatedAt = DateTime.UtcNow;
        audit.Add(userId, "page.document.saved", "page", page.Id.ToString(), null, ipAddress);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(page);
    }

    public async Task<BuilderPageResponse?> DuplicateAsync(
        Guid sourceId, DuplicatePageRequest request, int? userId, string? ipAddress,
        CancellationToken cancellationToken)
    {
        var source = await GetByIdAsync(sourceId, cancellationToken);
        if (source is null) return null;
        return await CreateAsync(new CreateBuilderPageRequest
        {
            Title = request.Title,
            Slug = request.Slug,
            Locale = request.Locale ?? source.Locale,
            Template = source.Template,
            PageGroupId = request.KeepTranslationGroup ? source.PageGroupId : null,
            IsHomepage = false,
            SeoData = source.SeoData,
            Document = source.Document
        }, userId, ipAddress, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var page = await context.Pages.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (page is null) return false;
        if (page.IsHomepage || page.Template is "contact" or "solutions")
            throw new InvalidOperationException(
                "This page supplies shared website data and cannot be deleted until its dependencies are moved to global components.");

        var navigationItems = await context.NavigationItems
            .Where(item => item.PageId == page.Id).ToListAsync(cancellationToken);
        context.NavigationItems.RemoveRange(navigationItems);
        var pagePath = $"/{page.Slug}";
        var redirects = await context.Redirects
            .Where(redirect => redirect.OldPath == pagePath || redirect.NewPath == pagePath)
            .ToListAsync(cancellationToken);
        context.Redirects.RemoveRange(redirects);
        var auditLogs = await context.AuditLogs
            .Where(log => log.EntityType == "page" && log.EntityId == page.Id.ToString())
            .ToListAsync(cancellationToken);
        context.AuditLogs.RemoveRange(auditLogs);

        var groupId = page.PageGroupId;
        context.Pages.Remove(page);
        if (groupId is not null && !await context.Pages.AnyAsync(
                candidate => candidate.PageGroupId == groupId && candidate.Id != page.Id,
                cancellationToken))
        {
            var group = await context.PageGroups.FindAsync([groupId.Value], cancellationToken);
            if (group is not null) context.PageGroups.Remove(group);
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static BuilderPageResponse ToResponse(Page page) => new(
        page.Id, page.PageGroupId, page.Title, page.Slug, page.Locale, page.Template,
        page.IsHomepage, page.IsSystemPage, page.SeoData,
        page.BuilderDocument ?? BuilderDocumentValidator.Empty(page.Template), page.UpdatedAt);

    private async Task EnsureSlugAvailableAsync(string slug, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await context.Pages.AnyAsync(page => page.Slug == slug && page.Id != excludedId, cancellationToken))
            throw new InvalidOperationException($"The URL '/{slug}' is already in use.");
    }

    private async Task<string> UniqueGroupNameAsync(string title, CancellationToken cancellationToken)
    {
        var candidate = title;
        var suffix = 2;
        while (await context.PageGroups.AnyAsync(group => group.InternalName == candidate, cancellationToken))
            candidate = $"{title} {suffix++}";
        return candidate;
    }

    private static string NormalizeLocale(string locale)
    {
        var normalized = locale.Trim().ToLowerInvariant();
        if (normalized is not ("en" or "ar")) throw new ArgumentException("Locale must be 'en' or 'ar'.");
        return normalized;
    }

    private static string NormalizeSlug(string locale, string? value, bool isHomepage)
    {
        locale = NormalizeLocale(locale);
        if (isHomepage) return locale;
        var slug = (value ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
        if (slug.StartsWith($"{locale}/", StringComparison.Ordinal)) slug = slug[(locale.Length + 1)..];
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("A non-homepage page requires a slug.");
        if (slug.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '/')))
            throw new ArgumentException("Slugs may contain letters, numbers, dashes, and slashes only.");
        return $"{locale}/{slug}";
    }

    private static string RequireText(string value, string message, int maximumLength)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0) throw new ArgumentException(message);
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.");
        return normalized;
    }
}
