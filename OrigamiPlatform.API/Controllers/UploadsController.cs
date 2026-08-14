using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Uploads;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    private readonly UploadImageHandler _uploadImage;

    public UploadsController(UploadImageHandler uploadImage)
        => _uploadImage = uploadImage;

    [HttpPost("image")]
    [Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] string folder, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new DomainException("File is required.");

        await using var stream = file.OpenReadStream();
        var url = await _uploadImage.HandleAsync(
            new UploadImageCommand(stream, file.FileName, file.ContentType, file.Length, folder),
            ct);

        return Ok(new { url });
    }
}
