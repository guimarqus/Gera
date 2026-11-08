using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gera.Application.Auth;
using Gera.Application.Catalog;
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
        var summary = await client.GetFromJsonAsync<ProfileSummaryDto>("/api/perfil/resumo");
        summary!.Nome.Should().Be("Maria Teste");
        summary.PapelAtivo.Should().Be("Cliente");

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
    public async Task ConcurrentRefreshCalls_ReturnSameRotationWithoutRevokingSession()
    {
        using var factory = await TestApi.CreateAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var login = await client.PostAsJsonAsync("/api/auth/register", TestApi.NewRegistration(suffix));
        var initialCookie = TestApi.GetCookie(login);

        var responses = await Task.WhenAll(
            TestApi.SendRefreshAsync(client, initialCookie),
            TestApi.SendRefreshAsync(client, initialCookie));
        responses.Select(r => r.StatusCode).Should().OnlyContain(code => code == HttpStatusCode.OK);
        var rotatedCookies = responses.Select(TestApi.GetCookie).ToArray();
        rotatedCookies[0].Should().Be(rotatedCookies[1]);
        var tokens = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<PublicAuthResponse>()));
        tokens[0]!.AccessToken.Should().Be(tokens[1]!.AccessToken);

        var next = await TestApi.SendRefreshAsync(client, rotatedCookies[0]);
        next.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RefreshReuseOutsideGraceRevokesWholeUserChain()
    {
        using var factory = await TestApi.CreateAsync(graceSeconds: 1);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var registration = TestApi.NewRegistration(suffix);
        var registrationResponse = await client.PostAsJsonAsync("/api/auth/register", registration);
        var originalCookie = TestApi.GetCookie(registrationResponse);
        var rotation = await TestApi.SendRefreshAsync(client, originalCookie);
        var rotatedCookie = TestApi.GetCookie(rotation);
        var secondLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest($"auth-{suffix}", registration.Senha));
        var secondSessionCookie = TestApi.GetCookie(secondLogin);

        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        (await TestApi.SendRefreshAsync(client, originalCookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await TestApi.SendRefreshAsync(client, rotatedCookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await TestApi.SendRefreshAsync(client, secondSessionCookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CategoriesEndpoint_ReturnsSeededTreeAnonymously()
    {
        using var factory = await TestApi.CreateAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var response = await client.GetAsync("/api/categorias");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<IReadOnlyList<CategoriaTreeDto>>();
        categories.Should().NotBeNullOrEmpty();
        categories![0].Id.Should().Be(1);
        categories[0].Subcategorias.Should().NotBeEmpty();
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
    private readonly int _graceSeconds;
    private bool _databaseMigrated;
    private string ConnectionString => $"Server=(localdb)\\MSSQLLocalDB;Database={_database};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public TestApi(int graceSeconds = 10) => _graceSeconds = graceSeconds;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            ["JwtSettings:Secret"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)),
            ["JwtSettings:Issuer"] = "GeraApi", ["JwtSettings:Audience"] = "GeraWeb", ["JwtSettings:ExpirationInMinutes"] = "15",
            ["Frontend:Origin"] = "http://localhost:5173", ["Auth:RefreshReuseGraceSeconds"] = _graceSeconds.ToString()
        }));
    }

    public static async Task<TestApi> CreateAsync(int graceSeconds = 10)
    {
        var factory = new TestApi(graceSeconds);
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

    public static async Task<HttpResponseMessage> SendRefreshAsync(HttpClient client, string cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }
}
