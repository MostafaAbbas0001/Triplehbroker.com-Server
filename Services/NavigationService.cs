using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Contracts;
using WebsiteAPI.Data;
using WebsiteAPI.Entities;

namespace WebsiteAPI.Services;

public sealed class NavigationService(ApplicationDbContext context, AuditService audit)
{
    private static readonly HashSet<string> LinkTypes =
        ["page", "url", "email", "phone", "whatsapp", "file", "none"];

    public async Task<NavigationMenuResponse?> GetAsync(
        string key,
        string locale,
        CancellationToken cancellationToken)
    {
        var menu = await context.NavigationMenus.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Key == key && candidate.Locale == locale,
            cancellationToken);
        if (menu is null) return null;
        var items = await context.NavigationItems.AsNoTracking()
            .Where(item => item.MenuId == menu.Id)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        var pageIds = items.Where(item => item.PageId != null).Select(item => item.PageId!.Value).Distinct();
        var pages = await context.Pages.AsNoTracking()
            .Where(page => pageIds.Contains(page.Id))
            .ToDictionaryAsync(page => page.Id, cancellationToken);
        var roots = items.Where(item => item.ParentItemId is null)
            .Select(item => MapItem(item, items, pages))
            .ToList();
        return new NavigationMenuResponse(menu.Id, menu.Name, menu.Key, menu.Locale, roots, menu.UpdatedAt);
    }

    public async Task<NavigationMenuResponse> SaveAsync(
        string key,
        string locale,
        SaveNavigationMenuRequest request,
        int? userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        key = key.Trim().ToLowerInvariant();
        locale = locale.Trim().ToLowerInvariant();
        if (key.Length is 0 or > 100) throw new ArgumentException("A valid menu key is required.");
        if (locale is not ("en" or "ar")) throw new ArgumentException("Locale must be 'en' or 'ar'.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            throw new ArgumentException("A valid menu name is required.");

        var flatRequests = Flatten(request.Items).ToList();
        if (flatRequests.Count > 200) throw new ArgumentException("A menu cannot contain more than 200 items.");
        var pageIds = flatRequests.Where(item => item.PageId != null).Select(item => item.PageId!.Value).Distinct().ToList();
        var existingPageIds = await context.Pages
            .Where(page => pageIds.Contains(page.Id))
            .Select(page => page.Id)
            .ToListAsync(cancellationToken);
        if (existingPageIds.Count != pageIds.Count)
            throw new ArgumentException("One or more navigation pages do not exist.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var menu = await context.NavigationMenus.SingleOrDefaultAsync(
            candidate => candidate.Key == key && candidate.Locale == locale,
            cancellationToken);
        if (menu is null)
        {
            menu = new NavigationMenu
            {
                Id = Guid.NewGuid(),
                Key = key,
                Locale = locale,
                CreatedAt = DateTime.UtcNow
            };
            context.NavigationMenus.Add(menu);
        }
        else
        {
            await context.NavigationItems
                .Where(item => item.MenuId == menu.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }
        menu.Name = request.Name.Trim();
        menu.UpdatedAt = DateTime.UtcNow;

        AddItems(menu, null, request.Items, 0);
        audit.Add(userId, "navigation.saved", "navigation_menu", menu.Id.ToString(), new { key, locale }, ipAddress);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetAsync(key, locale, cancellationToken))!;
    }

    private void AddItems(
        NavigationMenu menu,
        NavigationItem? parent,
        IReadOnlyList<SaveNavigationItemRequest> requests,
        int depth)
    {
        if (depth > 4) throw new ArgumentException("Navigation cannot be nested more than four levels.");
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var label = request.Label.Trim();
            var linkType = request.LinkType.Trim().ToLowerInvariant();
            if (label.Length is 0 or > 200) throw new ArgumentException("Every navigation item requires a label.");
            if (!LinkTypes.Contains(linkType)) throw new ArgumentException($"Unsupported link type '{linkType}'.");
            if (linkType == "page" && request.PageId is null)
                throw new ArgumentException($"Navigation item '{label}' requires a page.");
            if (linkType is not ("page" or "none") && string.IsNullOrWhiteSpace(request.Url))
                throw new ArgumentException($"Navigation item '{label}' requires a destination.");

            var item = new NavigationItem
            {
                Id = request.Id ?? Guid.NewGuid(),
                Menu = menu,
                MenuId = menu.Id,
                ParentItem = parent,
                ParentItemId = parent?.Id,
                PageId = request.PageId,
                Label = label,
                LinkType = linkType,
                Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim(),
                Anchor = string.IsNullOrWhiteSpace(request.Anchor) ? null : request.Anchor.Trim().TrimStart('#'),
                SortOrder = index,
                OpenInNewTab = request.OpenInNewTab,
                IsVisible = request.IsVisible,
                Style = request.Style is null
                    ? JsonDocument.Parse("{}")
                    : JsonDocument.Parse(request.Style.RootElement.GetRawText())
            };
            context.NavigationItems.Add(item);
            AddItems(menu, item, request.Children, depth + 1);
        }
    }

    private static NavigationItemResponse MapItem(
        NavigationItem item,
        IReadOnlyList<NavigationItem> allItems,
        IReadOnlyDictionary<Guid, Page> pages)
    {
        var href = item.LinkType switch
        {
            "page" when item.PageId is not null && pages.TryGetValue(item.PageId.Value, out var page) =>
                $"/{page.Slug}{Anchor(item.Anchor)}",
            "email" => $"mailto:{item.Url}",
            "phone" => $"tel:{item.Url}",
            "whatsapp" => $"https://wa.me/{new string((item.Url ?? string.Empty).Where(char.IsDigit).ToArray())}",
            "none" => null,
            _ => $"{item.Url}{Anchor(item.Anchor)}"
        };
        var children = allItems.Where(candidate => candidate.ParentItemId == item.Id)
            .OrderBy(candidate => candidate.SortOrder)
            .Select(candidate => MapItem(candidate, allItems, pages))
            .ToList();
        return new NavigationItemResponse(
            item.Id,
            item.Label,
            item.LinkType,
            item.PageId,
            item.Url,
            href,
            item.Anchor,
            item.SortOrder,
            item.OpenInNewTab,
            item.IsVisible,
            item.Style,
            children);
    }

    private static string Anchor(string? anchor) =>
        string.IsNullOrWhiteSpace(anchor) ? string.Empty : $"#{anchor}";

    private static IEnumerable<SaveNavigationItemRequest> Flatten(
        IEnumerable<SaveNavigationItemRequest> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in Flatten(item.Children)) yield return child;
        }
    }
}
