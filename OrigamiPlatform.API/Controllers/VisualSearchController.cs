using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Queries.VisualSearch;
using System.Threading;
using System.Threading.Tasks;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class VisualSearchController(SearchByObjectHandler searchHandler) : ControllerBase
{
    [HttpPost("search-by-object")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SearchByObject(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Vui lòng tải lên một bức ảnh hợp lệ.");
        }

        using var stream = file.OpenReadStream();
        var query = new SearchByObjectQuery(stream);

        var result = await searchHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }
}