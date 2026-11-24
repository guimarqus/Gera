using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gera.Application.Auth;
using Gera.Application.Ofertas;
using Gera.Domain.Enums;
using Gera.Domain.Entities;
using Gera.Application.Catalog;
using Gera.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Gera.IntegrationTests.Api;

public sealed class OfertaRascunhoTests
{
    [Fact]
    public async Task PublicacaoConcedePapelEPreservaPrivacidadeEmOfertaPublica()
    {
        using var factory = await TestApi.CreateAsync();
        using var proprietario = factory.CreateClient();
        using var visitante = factory.CreateClient();
        await AutenticarAsync(proprietario);
        await AutenticarAsync(visitante);

        using var criacao = await proprietario.PostAsync("/api/ofertas", null);
        criacao.EnsureSuccessStatusCode();
        var rascunho = (await criacao.Content.ReadFromJsonAsync<OfertaPrivadaDto>())!;
        var url = $"/api/ofertas/{rascunho.Id}";
        var dados = new EditarOfertaRequest(401, "Pintura residencial",
            "Pintura de paredes e tetos em residências.", TipoPreco.ACombinar, null,
            -3.119, -60.021, 20, "Manaus", null, "Segunda a sexta", null, rascunho.Versao);
        (await proprietario.PutAsJsonAsync(url, dados)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await visitante.PostAsync($"{url}/publicar", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await proprietario.PostAsync($"{url}/publicar", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.OfertaFotos.Add(new OfertaFoto { OfertaId = rascunho.Id, Url = "/foto-de-teste.webp", Ordem = 1 });
            await db.SaveChangesAsync();
        }

        using var publicacao = await proprietario.PostAsync($"{url}/publicar", null);
        publicacao.EnsureSuccessStatusCode();
        var publicada = (await publicacao.Content.ReadFromJsonAsync<OfertaPrivadaDto>())!;
        publicada.Status.Should().Be(StatusOferta.Ativo);
        publicada.PublicadoEm.Should().NotBeNull();
        (await proprietario.PostAsync($"{url}/publicar", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var perfil = await proprietario.GetFromJsonAsync<ProfileSummaryDto>("/api/perfil/resumo");
        perfil!.Papeis.Should().Contain("Prestador");
        var respostaPublica = await visitante.GetStringAsync(url);
        respostaPublica.Should().NotContain("latitude").And.NotContain("longitude");

        var pausa = await proprietario.PatchAsJsonAsync($"{url}/status", new AlterarStatusOfertaRequest(StatusOferta.Pausado));
        pausa.StatusCode.Should().Be(HttpStatusCode.OK);
        (await visitante.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var reativacao = await proprietario.PatchAsJsonAsync($"{url}/status", new AlterarStatusOfertaRequest(StatusOferta.Ativo));
        reativacao.StatusCode.Should().Be(HttpStatusCode.OK);
        var reativada = await reativacao.Content.ReadFromJsonAsync<OfertaPrivadaDto>();
        reativada!.PublicadoEm.Should().Be(publicada.PublicadoEm);
    }

    [Fact]
    public async Task ProprietarioPodeRetomarRascunhoComControleDeVersao()
    {
        using var factory = await TestApi.CreateAsync();
        using var proprietario = factory.CreateClient();
        using var visitante = factory.CreateClient();
        await AutenticarAsync(proprietario);
        await AutenticarAsync(visitante);

        using var criacao = await proprietario.PostAsync("/api/ofertas", null);
        criacao.StatusCode.Should().Be(HttpStatusCode.Created);
        var rascunho = (await criacao.Content.ReadFromJsonAsync<OfertaPrivadaDto>())!;
        rascunho.Status.Should().Be(StatusOferta.Rascunho);
        rascunho.CategoriaId.Should().BeNull();
        var url = $"/api/ofertas/{rascunho.Id}";
        var dados = new EditarOfertaRequest(1, "Pintura residencial", null, null, null,
            -3.119, -60.021, 20, "Manaus", null, null, null, rascunho.Versao,
            new() { ["tipoObra"] = "Residencial" }, ["Reforma", "réforma"], VersaoModelo: 1);

        (await visitante.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await visitante.PutAsJsonAsync(url, dados)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await proprietario.PutAsJsonAsync(url, dados)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await proprietario.PutAsJsonAsync(url, dados)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        var lista = await proprietario.GetFromJsonAsync<OfertaPrivadaDto[]>("/api/ofertas/minhas");
        lista.Should().ContainSingle(o => o.Id == rascunho.Id && o.Titulo == dados.Titulo);
        var retomado = await proprietario.GetFromJsonAsync<OfertaPrivadaDto>(url);
        retomado!.Caracteristicas!.Atributos.Should().Contain("tipoObra", "Residencial");
        retomado.Tags.Should().Equal("Reforma");
        (await proprietario.DeleteAsync(url)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await proprietario.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task AutenticarAsync(HttpClient client)
    {
        var cadastro = TestApi.NewRegistration(Guid.NewGuid().ToString("N")[..10]);
        using var resposta = await client.PostAsJsonAsync("/api/auth/register", cadastro);
        resposta.EnsureSuccessStatusCode();
        var sessao = (await resposta.Content.ReadFromJsonAsync<PublicAuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.AccessToken);
    }
}
