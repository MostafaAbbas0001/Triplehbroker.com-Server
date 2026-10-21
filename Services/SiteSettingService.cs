using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Contracts;
using WebsiteAPI.Data;

namespace WebsiteAPI.Services;

public class SiteSettingService(ApplicationDbContext context)
{
    public Task<List<SiteSettingResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        context.SiteSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Key)
            .Select(setting => new SiteSettingResponse(
                setting.Id,
                setting.Key,
                setting.Value,
                setting.UpdatedAt))
            .ToListAsync(cancellationToken);

    public Task<SiteSettingResponse?> GetAsync(
        string key,
        CancellationToken cancellationToken) =>
        context.SiteSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => new SiteSettingResponse(
                setting.Id,
                setting.Key,
                setting.Value,
                setting.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<SiteSettingResponse?> PatchAsync(
        string key,
        SiteSettingPatchRequest request,
        CancellationToken cancellationToken)
    {
        var setting = await context.SiteSettings.SingleOrDefaultAsync(
            candidate => candidate.Key == key,
            cancellationToken);

        if (setting is null)
        {
            return null;
        }

        if (request.Value is not null)
        {
            setting.Value = JsonDocument.Parse(request.Value.RootElement.GetRawText());
        }

        setting.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return new SiteSettingResponse(
            setting.Id,
            setting.Key,
            setting.Value,
            setting.UpdatedAt);
    }
}
