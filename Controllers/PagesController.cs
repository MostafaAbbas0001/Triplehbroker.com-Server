using Microsoft.AspNetCore.Mvc;
using WebsiteAPI.Contracts;
using WebsiteAPI.Services;

namespace WebsiteAPI.Controllers;

[ApiController]
[Route("api/pages")]
public class PagesController(PageService pageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PageResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await pageService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PageResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var page = await pageService.GetAsync(id, cancellationToken);
        return page is null ? NotFound() : Ok(page);
    }
}
