using FluentAssertions;
using Gera.Application.Auth;
using Gera.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gera.UnitTests.Auth;

public sealed class AuthSecurityTests
{
    private static readonly string Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private static readonly TokenService Tokens = new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["JwtSettings:Secret"] = Secret,
        ["JwtSettings:Issuer"] = "GeraApi", ["JwtSettings:Audience"] = "GeraWeb", ["JwtSettings:ExpirationInMinutes"] = "15"
    }).Build());

    [Fact]
    public void AccessToken_IsSignedJwt_WithShortExpiry()
    {
        var (token, expires) = Tokens.CreateAccessToken(new Usuario { Id = 42, UserName = "maria", Email = "maria@example.com", Nome = "Maria" });
        var now = DateTime.UtcNow;
        var pieces = token.Split('.');
        pieces.Should().HaveCount(3);
        expires.Should().BeAfter(now).And.BeOnOrBefore(now.AddMinutes(15));
        var payload = JsonDocument.Parse(Decode(pieces[1])).RootElement;
        payload.GetProperty("sub").GetString().Should().Be("42");
        payload.GetProperty("iss").GetString().Should().Be("GeraApi");
    }

    [Fact]
    public void AccessToken_SignatureValidates_AndTamperingIsRejected()
    {
        var (token, _) = Tokens.CreateAccessToken(new Usuario { Id = 8, UserName = "joao", Email = "joao@example.com", Nome = "João" });
        HasValidSignature(token).Should().BeTrue();
        var parts = token.Split('.');
        HasValidSignature($"{parts[0]}.{parts[1]}x.{parts[2]}").Should().BeFalse();
    }

    [Fact]
    public void RefreshTokens_AreRandom_AndOnlyHashIsStored()
    {
        var first = Tokens.CreateRefreshToken();
        var second = Tokens.CreateRefreshToken();
        first.Should().NotBe(second);
        Tokens.HashRefreshToken(first).Should().NotBe(first).And.HaveLength(64);
        Tokens.HashRefreshToken(first).Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first))));
    }

    [Fact]
    public void RefreshRotation_RevokesCurrentAndLinksReplacementHash()
    {
        var current = new RefreshToken { TokenHash = "old-hash" };
        var now = DateTime.UtcNow;
        RefreshTokenRotation.Rotate(current, "replacement-hash", now);
        current.RevogadoEm.Should().Be(now);
        current.SubstituidoPorHash.Should().Be("replacement-hash");
    }

    [Fact]
    public void ReusingRevokedToken_RevokesEveryStillActiveTokenInChain()
    {
        var now = DateTime.UtcNow;
        var original = new RefreshToken { RevogadoEm = now.AddMinutes(-1) };
        var activeOne = new RefreshToken(); var activeTwo = new RefreshToken();
        RefreshTokenRotation.RevokeActiveChain([original, activeOne, activeTwo], now);
        original.RevogadoEm.Should().Be(now.AddMinutes(-1));
        activeOne.RevogadoEm.Should().Be(now);
        activeTwo.RevogadoEm.Should().Be(now);
    }

    [Fact]
    public void RegistrationValidator_RejectsInvalidEmailPasswordPhoneAndMissingConsents()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("Maria", "invalido", "maria", "telefone1234", "fraca", false, false);
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().Contain(["Email", "Telefone", "Senha", "AceitouTermos", "AceitouPrivacidade"]);
    }

    private static byte[] Decode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '='));
    }

    private static bool HasValidSignature(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return false;
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"));
        try { return CryptographicOperations.FixedTimeEquals(expected, Decode(parts[2])); }
        catch (FormatException) { return false; }
    }
}
