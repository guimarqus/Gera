namespace Gera.Application.Auth;

public sealed record LoginRequest(string UsuarioOuEmail, string Senha);
public sealed record RegisterRequest(string Nome, string Email, string NomeUsuario, string Telefone, string Senha, bool AceitouTermos, bool AceitouPrivacidade);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NovaSenha);
public sealed record TokenPair(string AccessToken, DateTime ExpiraEm, string RefreshToken);
public sealed record PublicAuthResponse(string AccessToken, DateTime ExpiraEm);
public sealed record UserResponse(int Id, string Nome, string Email, string NomeUsuario, string? Telefone);
