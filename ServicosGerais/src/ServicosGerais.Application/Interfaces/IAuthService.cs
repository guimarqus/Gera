using ServicosGerais.Application.DTOs.Auth;

namespace ServicosGerais.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task EsqueciSenhaAsync(string email);
}

