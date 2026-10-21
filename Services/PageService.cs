using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Contracts;
using WebsiteAPI.Data;

namespace WebsiteAPI.Services;

public class PageService(ApplicationDbContext context)
{
    public Task<List<PageResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Pages
            .AsNoTracking()
            .OrderBy(page => page.Title)
            .Select(page => new PageResponse(
                page.Id,
                page.Title,
                page.Slug,
                page.SeoData,
                page.UpdatedAt))
            .ToListAsync(cancellationToken);

    public Task<PageResponse?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Pages
            .AsNoTracking()
            .Where(page => page.Id == id)
            .Select(page => new PageResponse(
                page.Id,
                page.Title,
                page.Slug,
                page.SeoData,
                page.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
}
