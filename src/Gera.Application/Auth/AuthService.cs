using Gera.Application.Interfaces;
using Gera.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Gera.Application.Auth;

public sealed class AuthService(
    UserManager<Usuario> userManager,
    SignInManager<Usuario> signInManager,
    IAppDbContext db,
    ITokenService tokenService,
    IEmailSender emailSender,
    IConfiguration configuration) : IAuthService
{
    private readonly int _refreshTokenDays = int.TryParse(configuration["JwtSettings:RefreshTokenExpirationInDays"], out var days) ? Math.Clamp(days, 1, 30) : 7;
    public async Task<TokenPair?> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(request.UsuarioOuEmail)
            ?? await userManager.FindByEmailAsync(request.UsuarioOuEmail);
        if (user is null) return null;
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Senha, lockoutOnFailure: true);
        if (!result.Succeeded || !user.Ativo) return null;
        return await IssueTokensAsync(user, ip, cancellationToken);
    }

    public async Task<TokenPair> RegisterAsync(RegisterRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = new Usuario
        {
            Nome = request.Nome.Trim(), Email = request.Email.Trim(), UserName = request.NomeUsuario.Trim(),
            PhoneNumber = request.Telefone.Trim(), AceitouTermos = true, TermosAceitosEm = now,
            AceitouPrivacidade = true, PrivacidadeAceitaEm = now, CriadoEm = now, AtualizadoEm = now
        };
        var result = await userManager.CreateAsync(user, request.Senha);
        if (!result.Succeeded)
            throw new AuthValidationException(result.Errors.Select(e => (TranslateIdentityField(e.Code), TranslateIdentityError(e.Code))));
        return await IssueTokensAsync(user, ip, cancellationToken);
    }

    public async Task<TokenPair?> RefreshAsync(string refreshToken, string? ip, CancellationToken cancellationToken = default)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var current = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (current is null) return null;
        if (current.RevogadoEm is not null)
        {
            var chain = await db.RefreshTokens.Where(x => x.UsuarioId == current.UsuarioId && x.RevogadoEm == null).ToListAsync(cancellationToken);
            RefreshTokenRotation.RevokeActiveChain(chain, DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        if (current.ExpiraEm <= DateTime.UtcNow) return null;
        var user = await userManager.FindByIdAsync(current.UsuarioId.ToString());
        if (user is null || !user.Ativo) return null;
        var replacement = CreateTokenPair(user);
        RefreshTokenRotation.Rotate(current, tokenService.HashRefreshToken(replacement.RefreshToken), DateTime.UtcNow);
        db.RefreshTokens.Add(CreateRefreshEntity(user.Id, replacement.RefreshToken, ip));
        await db.SaveChangesAsync(cancellationToken);
        return replacement;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = tokenService.HashRefreshToken(refreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null || token.RevogadoEm is not null) return;
        token.RevogadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null) return;
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encoded = Base64UrlToken.Encode(token);
        var origin = configuration["Frontend:Origin"] ?? "http://localhost:5173";
        var link = $"{origin}/redefinir-senha?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(encoded)}";
        await emailSender.SendPasswordResetAsync(user.Email!, link, cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null) return false;
        string decoded;
        try { decoded = Base64UrlToken.Decode(request.Token); }
        catch (FormatException) { return false; }
        var result = await userManager.ResetPasswordAsync(user, decoded, request.NovaSenha);
        return result.Succeeded;
    }

    public async Task<UserResponse?> GetUserAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new(user.Id, user.Nome, user.Email ?? "", user.UserName ?? "", user.PhoneNumber);
    }

    private async Task<TokenPair> IssueTokensAsync(Usuario user, string? ip, CancellationToken cancellationToken)
    {
        var pair = CreateTokenPair(user);
        db.RefreshTokens.Add(CreateRefreshEntity(user.Id, pair.RefreshToken, ip));
        await db.SaveChangesAsync(cancellationToken);
        return pair;
    }

    private TokenPair CreateTokenPair(Usuario user)
    {
        var (access, expires) = tokenService.CreateAccessToken(user);
        return new(access, expires, tokenService.CreateRefreshToken());
    }

    private RefreshToken CreateRefreshEntity(int userId, string refresh, string? ip)
    {
        var now = DateTime.UtcNow;
        return new RefreshToken
        {
            UsuarioId = userId, TokenHash = tokenService.HashRefreshToken(refresh), CriadoEm = now,
            ExpiraEm = now.AddDays(_refreshTokenDays), IpCriacao = ip
        };
    }

    private static string TranslateIdentityError(string code) => code switch
    {
        "DuplicateEmail" => "Este e-mail já está cadastrado.",
        "DuplicateUserName" => "Este nome de usuário já está em uso.",
        "InvalidEmail" => "Informe um e-mail válido.",
        "PasswordTooShort" => "A senha deve ter pelo menos 8 caracteres.",
        "PasswordRequiresDigit" => "A senha deve conter pelo menos um número.",
        "PasswordRequiresLower" or "PasswordRequiresUpper" => "A senha deve conter pelo menos uma letra.",
        "InvalidUserName" => "O nome de usuário contém caracteres inválidos.",
        _ => "Não foi possível criar a conta com esses dados."
    };

    private static string TranslateIdentityField(string code) => code switch
    {
        "DuplicateEmail" or "InvalidEmail" => "Email",
        "DuplicateUserName" or "InvalidUserName" => "NomeUsuario",
        "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or "PasswordRequiresUpper" => "Senha",
        _ => "Cadastro"
    };
}

public sealed class AuthValidationException(IEnumerable<(string Code, string Description)> errors)
    : Exception("Não foi possível criar a conta.")
{
    public IReadOnlyList<(string Code, string Description)> Errors { get; } = errors.ToArray();
}
