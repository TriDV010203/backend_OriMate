using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Shop;
using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Queries.Shop;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/shop")]
public class ShopController : ControllerBase
{
    private readonly GetShopLinksHandler _getShopLinks;
    private readonly CreateShopLinkHandler _createShopLink;
    private readonly UpdateShopLinkHandler _updateShopLink;

    public ShopController(
        GetShopLinksHandler getShopLinks,
        CreateShopLinkHandler createShopLink,
        UpdateShopLinkHandler updateShopLink)
        => (_getShopLinks, _createShopLink, _updateShopLink) = (getShopLinks, createShopLink, updateShopLink);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetShopLinks(CancellationToken ct)
    {
        var result = await _getShopLinks.HandleAsync(new GetShopLinksQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateShopLink(CreateShopLinkRequest req, CancellationToken ct)
    {
        var result = await _createShopLink.HandleAsync(new CreateShopLinkCommand(GetCurrentUserId(), req), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateShopLink(Guid id, UpdateShopLinkRequest req, CancellationToken ct)
    {
        var result = await _updateShopLink.HandleAsync(new UpdateShopLinkCommand(GetCurrentUserId(), id, req), ct);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new ForbiddenException("User identity not found in token.");
        return Guid.Parse(sub);
    }
}
