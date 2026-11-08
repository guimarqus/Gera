using Gera.Domain.Entities;

namespace Gera.Application.Auth;

public interface IAuthService
{
    Task<TokenPair?> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<TokenPair> RegisterAsync(RegisterRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<TokenPair?> RefreshAsync(string refreshToken, string? ip, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetUserAsync(int userId);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(Usuario usuario);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}
