using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsiteAPI.Contracts;
using WebsiteAPI.Services;

namespace WebsiteAPI.Controllers;

[ApiController]
[Route("api/site-settings")]
public class SiteSettingsController(SiteSettingService siteSettingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SiteSettingResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await siteSettingService.GetAllAsync(cancellationToken));

    [HttpGet("{key}")]
    public async Task<ActionResult<SiteSettingResponse>> Get(
        string key,
        CancellationToken cancellationToken)
    {
        var setting = await siteSettingService.GetAsync(key, cancellationToken);
        return setting is null ? NotFound() : Ok(setting);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{key}")]
    public async Task<ActionResult<SiteSettingResponse>> Patch(
        string key,
        SiteSettingPatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Value is null)
        {
            return BadRequest(new { message = "Provide a value to update." });
        }

        var setting = await siteSettingService.PatchAsync(key, request, cancellationToken);
        return setting is null ? NotFound() : Ok(setting);
    }
}
