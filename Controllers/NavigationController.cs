using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsiteAPI.Contracts;
using WebsiteAPI.Services;

namespace WebsiteAPI.Controllers;

[ApiController]
[Route("api/navigation")]
public sealed class NavigationController(NavigationService navigation) : ControllerBase
{
    [HttpGet("{locale}/{key}")]
    public async Task<ActionResult<NavigationMenuResponse>> Get(
        string locale,
        string key,
        CancellationToken cancellationToken)
    {
        var menu = await navigation.GetAsync(key, locale, cancellationToken);
        return menu is null ? NotFound() : Ok(menu);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{locale}/{key}")]
    public async Task<ActionResult<NavigationMenuResponse>> Save(
        string locale,
        string key,
        SaveNavigationMenuRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await navigation.SaveAsync(
                key,
                locale,
                request,
                int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
