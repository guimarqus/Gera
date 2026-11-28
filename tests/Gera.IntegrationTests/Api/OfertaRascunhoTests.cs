using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gera.Application.Auth;
using Gera.Application.Ofertas;
using Gera.Domain.Enums;
using Gera.Application.Catalog;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

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
            -3.119, -60.021, 20, "Manaus", "Centro", "Segunda a sexta", null, rascunho.Versao);
        (await proprietario.PutAsJsonAsync(url, dados)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await visitante.PostAsync($"{url}/publicar", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await proprietario.PostAsync($"{url}/publicar", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await visitante.GetAsync($"{url}/fotos")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var vazias = (await proprietario.GetFromJsonAsync<FotosOfertaDto>($"{url}/fotos"))!;
        using var limpeza = new LimpezaFotos(factory.Services.GetRequiredService<IArmazenamentoMidia>(), rascunho.Id);
        var primeira = await EnviarFotoAsync(proprietario, url, vazias.Versao);
        limpeza.Adicionar(primeira);
        var fotos = await EnviarFotoAsync(proprietario, url, primeira.Versao);
        limpeza.Adicionar(fotos);
        var urlArquivo = fotos.Fotos[0].Url;
        (await visitante.GetAsync(urlArquivo)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await proprietario.GetAsync(urlArquivo)).StatusCode.Should().Be(HttpStatusCode.OK);
        var fotoIds = fotos.Fotos.Reverse().Select(foto => foto.Id).ToArray();
        using var ordem = await proprietario.PatchAsJsonAsync($"{url}/fotos/ordem", new OrdenarFotosRequest(fotoIds, fotos.Versao));
        ordem.EnsureSuccessStatusCode();
        var fotosOrdenadas = (await ordem.Content.ReadFromJsonAsync<FotosOfertaDto>())!;
        fotosOrdenadas.Fotos.Select(foto => foto.Id).Should().Equal(fotoIds);
        using var capa = await proprietario.PatchAsJsonAsync($"{url}/fotos/{fotoIds[0]}/capa", new AlterarFotoRequest(fotosOrdenadas.Versao));
        capa.EnsureSuccessStatusCode();
        var fotosComCapa = (await capa.Content.ReadFromJsonAsync<FotosOfertaDto>())!;
        fotosComCapa.Fotos.Should().ContainSingle(foto => foto.EhCapa && foto.Id == fotoIds[0]);

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
        (await visitante.GetAsync(urlArquivo)).StatusCode.Should().Be(HttpStatusCode.OK);

        var feed = await visitante.GetFromJsonAsync<OfertaFeedPaginaDto>(
            "/api/ofertas/feed?busca=Pintura&categoriaId=4&disponibilidade=Segunda&latitude=-3.119&longitude=-60.021&raioKm=5&ordem=proximidade");
        feed!.Ofertas.Should().ContainSingle(oferta => oferta.Id == rascunho.Id && oferta.DistanciaKm == 0);
        feed.Ofertas[0].FotoCapaUrl.Should().Be(fotosComCapa.Fotos.Single(foto => foto.EhCapa).UrlMiniatura);
        feed.Ofertas[0].EhFavorita.Should().BeFalse();

        using var visitanteAnonimo = factory.CreateClient();
        var detalhe = await visitanteAnonimo.GetFromJsonAsync<OfertaDetalhePublicoDto>($"{url}/detalhes");
        detalhe!.PrestadorId.Should().Be(feed.Ofertas[0].PrestadorId);
        detalhe.Categoria.Should().Be("Pintura Residencial Interna");
        detalhe.Fotos.Should().ContainSingle(foto => foto.EhCapa);
        var detalheJson = await visitanteAnonimo.GetStringAsync($"{url}/detalhes");
        detalheJson.Should().NotContain("latitude").And.NotContain("longitude").And.NotContain("email").And.NotContain("telefone");
        var perfilPublico = await visitanteAnonimo.GetFromJsonAsync<PrestadorPublicoDto>($"/api/prestadores/{detalhe.PrestadorId}");
        perfilPublico!.TotalOfertas.Should().Be(1);
        perfilPublico.Ofertas.Should().ContainSingle(oferta => oferta.Id == rascunho.Id);

        var feedJson = await visitante.GetStringAsync("/api/ofertas/feed?busca=Pintura&cidade=Centro");
        feedJson.Should().NotContain("latitude").And.NotContain("longitude");

        (await visitante.PutAsync($"{url}/favorito", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await visitante.PutAsync($"{url}/favorito", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var favoritada = await visitante.GetFromJsonAsync<OfertaFeedPaginaDto>("/api/ofertas/feed?cidade=Centro");
        favoritada!.Ofertas.Should().ContainSingle(oferta => oferta.Id == rascunho.Id && oferta.EhFavorita);
        (await visitante.DeleteAsync($"{url}/favorito")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await visitante.DeleteAsync($"{url}/favorito")).StatusCode.Should().Be(HttpStatusCode.NoContent);

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
        (await visitante.GetAsync($"{url}/detalhes")).StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private static async Task<FotosOfertaDto> EnviarFotoAsync(HttpClient client, string url, string versao)
    {
        using var bitmap = new SKBitmap(16, 16);
        bitmap.Erase(SKColors.Teal);
        using var imagem = SKImage.FromBitmap(bitmap);
        using var png = imagem.Encode(SKEncodedImageFormat.Png, 100);
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(png.ToArray()), "arquivo", "teste.png" },
            { new StringContent(versao), "versao" }
        };
        using var resposta = await client.PostAsync($"{url}/fotos", form);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<FotosOfertaDto>())!;
    }

    private sealed class LimpezaFotos(IArmazenamentoMidia midia, int ofertaId) : IDisposable
    {
        private readonly HashSet<string> _urls = [];

        public void Adicionar(FotosOfertaDto fotos)
        {
            foreach (var foto in fotos.Fotos)
            {
                _urls.Add(foto.Url);
                if (foto.UrlMiniatura is not null)
                    _urls.Add(foto.UrlMiniatura);
            }
        }

        public void Dispose()
        {
            foreach (var url in _urls)
                midia.RemoverFoto(ofertaId, url);
        }
    }
}
