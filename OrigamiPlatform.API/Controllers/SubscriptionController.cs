using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Subscriptions;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Queries.Subscriptions;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ConfigureVipTierHandler _configureVipTier;
    private readonly SubscribeHandler _subscribe;
    private readonly ConfirmPaymentHandler _confirmPayment;
    private readonly RejectPaymentHandler _rejectPayment;
    private readonly GetMySubscriptionsHandler _getMySubscriptions;
    private readonly GetCreatorRevenueHandler _getCreatorRevenue;

    public SubscriptionController(
        ConfigureVipTierHandler configureVipTier,
        SubscribeHandler subscribe,
        ConfirmPaymentHandler confirmPayment,
        RejectPaymentHandler rejectPayment,
        GetMySubscriptionsHandler getMySubscriptions,
        GetCreatorRevenueHandler getCreatorRevenue)
    {
        _configureVipTier = configureVipTier;
        _subscribe = subscribe;
        _confirmPayment = confirmPayment;
        _rejectPayment = rejectPayment;
        _getMySubscriptions = getMySubscriptions;
        _getCreatorRevenue = getCreatorRevenue;
    }

    [HttpPut("vip-tier")]
    public async Task<IActionResult> ConfigureVipTier(ConfigureVipTierRequest request, CancellationToken ct)
    {
        var result = await _configureVipTier.HandleAsync(
            new ConfigureVipTierCommand(GetCurrentUserId(), request.Price, request.IsActive),
            ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(SubscribeRequest request, CancellationToken ct)
    {
        var result = await _subscribe.HandleAsync(
            new SubscribeCommand(GetCurrentUserId(), request.CreatorId, request.ReferenceCode),
            ct);

        return Ok(result);
    }

    [HttpPost("transactions/{transactionId:guid}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfirmPayment(Guid transactionId, CancellationToken ct)
    {
        var result = await _confirmPayment.HandleAsync(
            new ConfirmPaymentCommand(GetCurrentUserId(), transactionId),
            ct);

        return Ok(result);
    }

    [HttpPost("transactions/{transactionId:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectPayment(
        Guid transactionId,
        RejectPaymentRequest request,
        CancellationToken ct)
    {
        var result = await _rejectPayment.HandleAsync(
            new RejectPaymentCommand(GetCurrentUserId(), transactionId, request.AdminNote),
            ct);

        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMySubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getMySubscriptions.HandleAsync(
            new GetMySubscriptionsQuery(GetCurrentUserId(), page, pageSize),
            ct);

        return Ok(result);
    }

    [HttpGet("creators/{creatorId:guid}/revenue")]
    public async Task<IActionResult> GetCreatorRevenue(Guid creatorId, CancellationToken ct)
    {
        var result = await _getCreatorRevenue.HandleAsync(
            new GetCreatorRevenueQuery(creatorId, GetCurrentUserId()),
            ct);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new ForbiddenException("Invalid user token.");

        return userId;
    }
}
