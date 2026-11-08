using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gera.Application.Auth;
using Gera.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gera.IntegrationTests.Api;

public sealed class AuthFlowTests
{
    [Fact]
    public async Task Register_Login_Me_Refresh_Logout_Works()
    {
        using var factory = await TestApi.CreateAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var response = await client.PostAsJsonAsync("/api/auth/register", TestApi.NewRegistration(suffix));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest($"auth-{suffix}", "Senha1234"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await loginResponse.Content.ReadFromJsonAsync<PublicAuthResponse>())!;
        var oldCookie = TestApi.GetCookie(loginResponse);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/eu");
        me!.Email.Should().Be($"auth-{suffix}@example.com");

        client.DefaultRequestHeaders.Authorization = null;
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", oldCookie);
        var refreshResponse = await client.SendAsync(refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedCookie = TestApi.GetCookie(refreshResponse);
        rotatedCookie.Should().NotBe(oldCookie);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", rotatedCookie);
        (await client.SendAsync(logoutRequest)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        reuseRequest.Headers.Add("Cookie", rotatedCookie);
        (await client.SendAsync(reuseRequest)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FiveWrongPasswords_LockAccountForSubsequentCorrectLogin()
    {
        using var factory = await TestApi.CreateAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var registration = TestApi.NewRegistration(suffix);
        (await client.PostAsJsonAsync("/api/auth/register", registration)).StatusCode.Should().Be(HttpStatusCode.OK);

        for (var i = 0; i < 5; i++)
        {
            var bad = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest($"auth-{suffix}", "WrongPass123"));
            bad.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await bad.Content.ReadAsStringAsync()).Should().Contain("Usuário ou senha incorretos");
        }
        var locked = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest($"auth-{suffix}", registration.Senha));
        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

internal sealed class TestApi : WebApplicationFactory<Program>
{
    private readonly string _database = $"GeraAuthTest_{Guid.NewGuid():N}";
    private bool _databaseMigrated;
    private string ConnectionString => $"Server=(localdb)\\MSSQLLocalDB;Database={_database};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            ["JwtSettings:Secret"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)),
            ["JwtSettings:Issuer"] = "GeraApi", ["JwtSettings:Audience"] = "GeraWeb", ["JwtSettings:ExpirationInMinutes"] = "15",
            ["Frontend:Origin"] = "http://localhost:5173"
        }));
    }

    public static async Task<TestApi> CreateAsync()
    {
        var factory = new TestApi();
        _ = factory.Services;
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        factory._databaseMigrated = true;
        return factory;
    }

    public new void Dispose()
    {
        if (_databaseMigrated)
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeleted();
        }
        base.Dispose();
    }

    public static RegisterRequest NewRegistration(string suffix) => new("Maria Teste", $"auth-{suffix}@example.com", $"auth-{suffix}", "11999998888", "Senha1234", true, true);

    public static string GetCookie(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("gera_refresh=", StringComparison.Ordinal));
        return header.Split(';', 2)[0];
    }
}
