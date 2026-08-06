using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrigamiPlatform.API.Options;
using OrigamiPlatform.Application.Commands.Webhooks;
using OrigamiPlatform.Application.DTOs.Webhooks;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ProcessSePayWebhookHandler _processWebhook;
    private readonly SePayOptions _sePayOptions;

    public WebhooksController(ProcessSePayWebhookHandler processWebhook, IOptions<SePayOptions> sePayOptions)
        => (_processWebhook, _sePayOptions) = (processWebhook, sePayOptions.Value);

    /// <summary>
    /// POST /api/webhooks/sepay — SePay "money in" notification. Signature (Authorization: Apikey ...)
    /// is verified BEFORE the raw body is parsed or touched in any way (CLAUDE.md webhook rule).
    /// </summary>
    [HttpPost("sepay")]
    public async Task<IActionResult> SePay(CancellationToken ct)
    {
        if (!IsAuthorized(Request.Headers["Authorization"].ToString()))
            return Unauthorized();

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);

        var payload = JsonSerializer.Deserialize<SePayWebhookPayloadDto>(rawBody, JsonOptions);
        if (payload is null)
            return BadRequest(new { success = false });

        await _processWebhook.HandleAsync(
            new ProcessSePayWebhookCommand(
                payload.Id,
                payload.Gateway,
                payload.TransferType,
                payload.TransferAmount,
                payload.Content,
                payload.Code,
                rawBody),
            ct);

        // Always ack 200 once authenticated + parsed — NoMatch/AmountMismatch are audited via
        // SePayWebhookLog, not SePay's problem to retry.
        return Ok(new { success = true });
    }

    private bool IsAuthorized(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(_sePayOptions.WebhookApiKey) || string.IsNullOrEmpty(authorizationHeader))
            return false;

        var expected = Encoding.UTF8.GetBytes($"Apikey {_sePayOptions.WebhookApiKey}");
        var actual = Encoding.UTF8.GetBytes(authorizationHeader);

        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
