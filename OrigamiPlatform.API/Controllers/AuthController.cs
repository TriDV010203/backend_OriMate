using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.DTOs.Auth;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _register;
    private readonly LoginHandler _login;

    public AuthController(RegisterUserHandler register, LoginHandler login)
        => (_register, _login) = (register, login);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _register.HandleAsync(
            new RegisterUserCommand(request.Email, request.Password), ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _login.HandleAsync(
            new LoginCommand(request.Email, request.Password), ct);
        return Ok(result);
    }
}
