using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsiteAPI.Contracts;
using WebsiteAPI.Services;

namespace WebsiteAPI.Controllers;

[ApiController]
[Route("api/themes")]
public sealed class ThemesController(ThemeService themes) : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult<ThemeResponse>> GetActive(CancellationToken cancellationToken)
    {
        var theme = await themes.GetActiveAsync(cancellationToken);
        return theme is null ? NotFound() : Ok(theme);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ThemeResponse>> Update(
        Guid id,
        UpdateThemeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var theme = await themes.UpdateAsync(
                id,
                request,
                int.TryParse(User.FindFirstValue("sub"), out var userId) ? userId : null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
            return theme is null ? NotFound() : Ok(theme);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
