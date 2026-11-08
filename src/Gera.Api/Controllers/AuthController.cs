using System.Security.Claims;
using Gera.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gera.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(IAuthService authService, IConfiguration configuration) : ControllerBase
{
    private const string CookieName = "gera_refresh";
    private readonly int _refreshTokenDays = int.TryParse(configuration["JwtSettings:RefreshTokenExpirationInDays"], out var days) ? Math.Clamp(days, 1, 30) : 7;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var tokens = await authService.LoginAsync(request, Ip, cancellationToken);
        if (tokens is null) return Unauthorized(new { mensagem = "Usuário ou senha incorretos" });
        SetRefreshCookie(tokens.RefreshToken);
        return Ok(new PublicAuthResponse(tokens.AccessToken, tokens.ExpiraEm));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await authService.RegisterAsync(request, Ip, cancellationToken);
            SetRefreshCookie(tokens.RefreshToken);
            return Ok(new PublicAuthResponse(tokens.AccessToken, tokens.ExpiraEm));
        }
        catch (AuthValidationException ex)
        {
            return Conflict(new { erros = ex.Errors.Select(e => new { campo = e.Code, mensagem = e.Description }) });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var cookie = Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(cookie)) return Unauthorized();
        var tokens = await authService.RefreshAsync(cookie, Ip, cancellationToken);
        if (tokens is null) { ClearRefreshCookie(); return Unauthorized(); }
        SetRefreshCookie(tokens.RefreshToken);
        return Ok(new PublicAuthResponse(tokens.AccessToken, tokens.ExpiraEm));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies[CookieName], cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok(new { mensagem = "Se o e-mail estiver cadastrado, você receberá instruções para redefinir sua senha." });
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        return result ? Ok(new { mensagem = "Senha redefinida com sucesso." }) : BadRequest(new { mensagem = "Não foi possível redefinir a senha. Verifique o e-mail e o token." });
    }

    [HttpGet("eu")]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(id, out var userId)) return Unauthorized();
        var user = await authService.GetUserAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();
    private void SetRefreshCookie(string token) => Response.Cookies.Append(CookieName, token, new CookieOptions
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/api/auth",
        Expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenDays), IsEssential = true
    });
    private void ClearRefreshCookie() => Response.Cookies.Delete(CookieName, new CookieOptions
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/api/auth", IsEssential = true
    });
}
