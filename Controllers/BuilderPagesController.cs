using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsiteAPI.Contracts;
using WebsiteAPI.Services;

namespace WebsiteAPI.Controllers;

[ApiController]
[Route("api/builder/pages")]
public sealed class BuilderPagesController(BuilderPageService pages) : ControllerBase
{
    [HttpGet("public")]
    public async Task<ActionResult<BuilderPageResponse>> GetPublic(
        [FromQuery] string locale,
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        var page = await pages.GetByPathAsync(locale, path, cancellationToken);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpGet("public/solution-summaries")]
    public async Task<ActionResult<List<SolutionPageSummary>>> GetSolutionSummaries(
        [FromQuery] string locale,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await pages.GetSolutionSummariesAsync(locale, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<BuilderPageSummary>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await pages.GetAllAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BuilderPageResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var page = await pages.GetByIdAsync(id, cancellationToken);
        return page is null ? NotFound() : Ok(page);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<BuilderPageResponse>> Create(
        CreateBuilderPageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await pages.CreateAsync(
                request, UserId(), IpAddress(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BuilderPageResponse>> Update(
        Guid id,
        UpdateBuilderPageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await pages.UpdateMetadataAsync(
                id, request, UserId(), IpAddress(), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/document")]
    public async Task<ActionResult<BuilderPageResponse>> SaveDocument(
        Guid id,
        SavePageDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await pages.SaveDocumentAsync(
                id, request.Document, UserId(), IpAddress(), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<BuilderPageResponse>> Duplicate(
        Guid id,
        DuplicatePageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var duplicated = await pages.DuplicateAsync(
                id, request, UserId(), IpAddress(), cancellationToken);
            return duplicated is null
                ? NotFound()
                : CreatedAtAction(nameof(GetById), new { id = duplicated.Id }, duplicated);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await pages.DeleteAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    private int? UserId() =>
        int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    private string? IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
