using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebsiteAPI.Contracts;
using WebsiteAPI.Services;
using System.Security.Claims;

namespace WebsiteAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/uploads")]
public class UploadsController(UploadService uploadService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UploadResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await uploadService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadService.MaximumFileSize + 1024 * 1024)]
    public async Task<ActionResult<UploadResponse>> Upload(
        [FromForm] IFormFile image,
        CancellationToken cancellationToken)
    {
        try
        {
            var uploaded = await uploadService.UploadAsync(
                image,
                cancellationToken,
                int.TryParse(User.FindFirstValue("sub"), out var userId) ? userId : null);
            return Created(uploaded.Url, uploaded);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(string fileName, CancellationToken cancellationToken) =>
        await uploadService.DeleteAsync(fileName, cancellationToken) ? NoContent() : NotFound();

}
