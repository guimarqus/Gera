using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Gera.Application.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Gera.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace Gera.IntegrationTests.Api;

public sealed class AdminAuthorizationTests
{
    [Fact]
    public async Task SomenteMembroAtivoDoPapelAdministradorAcessaPainel()
    {
        using var factory = await TestApi.CreateAsync();
        using var client = factory.CreateClient();
        var userId = await AutenticarAsync(client);
        using var denied = await client.GetAsync("/api/admin/resumo");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<Gera.Domain.Entities.Usuario>>();
            var user = await manager.FindByIdAsync(userId.ToString());
            Assert.NotNull(user);
            var result = await manager.AddToRoleAsync(user!, "Administrador");
            Assert.True(result.Succeeded);
        }
        using var allowed = await client.GetAsync("/api/admin/resumo");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    private static async Task<int> AutenticarAsync(HttpClient client)
    {
        var registration = TestApi.NewRegistration(Guid.NewGuid().ToString("N")[..10]);
        using var response = await client.PostAsJsonAsync("/api/auth/register", registration);
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<PublicAuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (await client.GetFromJsonAsync<UserResponse>("/api/auth/eu"))!.Id;
    }
}
