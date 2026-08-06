using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Subscriptions;
using OrigamiPlatform.Application.DTOs.Subscriptions;
using OrigamiPlatform.Application.Queries.Subscriptions;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ConfigureVipTierHandler _configureVipTier;
    private readonly SubscribeHandler _subscribe;
    private readonly GetMySubscriptionsHandler _getMySubscriptions;
    private readonly GetCreatorRevenueHandler _getCreatorRevenue;
    private readonly GetAllTransactionsHandler _getAllTransactions;
    private readonly GetPlatformRevenueHandler _getPlatformRevenue;
    private readonly GetMyVipTierHandler _getMyVipTier;
    private readonly GetTransactionByIdHandler _getTransactionById;

    public SubscriptionController(
        ConfigureVipTierHandler configureVipTier,
        SubscribeHandler subscribe,
        GetMySubscriptionsHandler getMySubscriptions,
        GetCreatorRevenueHandler getCreatorRevenue,
        GetAllTransactionsHandler getAllTransactions,
        GetPlatformRevenueHandler getPlatformRevenue,
        GetMyVipTierHandler getMyVipTier,
        GetTransactionByIdHandler getTransactionById)
    {
        _configureVipTier = configureVipTier;
        _subscribe = subscribe;
        _getMySubscriptions = getMySubscriptions;
        _getCreatorRevenue = getCreatorRevenue;
        _getAllTransactions = getAllTransactions;
        _getPlatformRevenue = getPlatformRevenue;
        _getMyVipTier = getMyVipTier;
        _getTransactionById = getTransactionById;
    }

    [HttpGet("vip-tier")]
    public async Task<IActionResult> GetMyVipTier(CancellationToken ct)
    {
        var result = await _getMyVipTier.HandleAsync(new GetMyVipTierQuery(GetCurrentUserId()), ct);
        return Ok(result);
    }

    [HttpPut("vip-tier")]
    public async Task<IActionResult> ConfigureVipTier(ConfigureVipTierRequest request, CancellationToken ct)
    {
        var result = await _configureVipTier.HandleAsync(
            new ConfigureVipTierCommand(GetCurrentUserId(), request.IsActive),
            ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(SubscribeRequest request, CancellationToken ct)
    {
        var result = await _subscribe.HandleAsync(
            new SubscribeCommand(GetCurrentUserId(), request.CreatorId),
            ct);

        return Ok(result);
    }

    /// <summary>GET /api/subscriptions/transactions/{id} — buyer polls their own Transaction status while waiting for the SePay webhook to auto-confirm it.</summary>
    [HttpGet("transactions/{transactionId:guid}")]
    public async Task<IActionResult> GetTransaction(Guid transactionId, CancellationToken ct)
    {
        var result = await _getTransactionById.HandleAsync(
            new GetTransactionByIdQuery(transactionId, GetCurrentUserId()),
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

    [HttpGet("transactions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] TransactionStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getAllTransactions.HandleAsync(
            new GetAllTransactionsQuery(status, page, pageSize),
            ct);

        return Ok(result);
    }

    [HttpGet("admin/revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPlatformRevenue(CancellationToken ct)
    {
        var result = await _getPlatformRevenue.HandleAsync(new GetPlatformRevenueQuery(), ct);
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
