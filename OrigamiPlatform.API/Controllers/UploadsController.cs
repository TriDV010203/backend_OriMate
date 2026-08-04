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
    private readonly UploadModel3DHandler _uploadModel3D;

    public UploadsController(UploadImageHandler uploadImage, UploadModel3DHandler uploadModel3D)
        => (_uploadImage, _uploadModel3D) = (uploadImage, uploadModel3D);

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

    [HttpPost("model-3d")]
    [Authorize]
    [RequestSizeLimit(UploadModel3DHandler.MaxRequestSizeBytes)]
    public async Task<IActionResult> UploadModel3D(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new DomainException("File is required.");

        await using var stream = file.OpenReadStream();
        var url = await _uploadModel3D.HandleAsync(
            new UploadModel3DCommand(stream, file.FileName, file.ContentType, file.Length),
            ct);

        return Ok(new { url });
    }
}
