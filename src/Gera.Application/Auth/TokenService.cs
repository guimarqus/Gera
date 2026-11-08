using Gera.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gera.Application.Auth;

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    private readonly string _secret = configuration["JwtSettings:Secret"] ?? string.Empty;
    private readonly string _issuer = configuration["JwtSettings:Issuer"] ?? "GeraApi";
    private readonly string _audience = configuration["JwtSettings:Audience"] ?? "GeraWeb";
    private readonly int _minutes = int.TryParse(configuration["JwtSettings:ExpirationInMinutes"], out var value) ? value : 15;

    public (string Token, DateTime ExpiresAt) CreateAccessToken(Usuario usuario)
    {
        if (Encoding.UTF8.GetByteCount(_secret) < 32) throw new InvalidOperationException("Configure JwtSettings:Secret com ao menos 32 bytes via User Secrets.");
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(Math.Clamp(_minutes, 1, 15));
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["iss"] = _issuer, ["aud"] = _audience,
            ["iat"] = new DateTimeOffset(now).ToUnixTimeSeconds(), ["nbf"] = new DateTimeOffset(now).ToUnixTimeSeconds(),
            ["exp"] = new DateTimeOffset(expires).ToUnixTimeSeconds(), ["sub"] = usuario.Id.ToString(),
            ["email"] = usuario.Email ?? string.Empty, ["unique_name"] = usuario.UserName ?? string.Empty,
            ["name"] = usuario.Nome
        }));
        var unsigned = $"{header}.{payload}";
        var signature = Base64Url(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_secret), Encoding.ASCII.GetBytes(unsigned)));
        return ($"{unsigned}.{signature}", expires);
    }

    public string CreateRefreshToken() => Base64Url(RandomNumberGenerator.GetBytes(64));
    public string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public static class Base64UrlToken
{
    public static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static string Decode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=')));
    }
}
