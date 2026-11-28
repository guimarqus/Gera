using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gera.Application.Auth;
using Gera.Application.Confianca;
using Gera.Application.Solicitacoes;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gera.IntegrationTests.Api;

public sealed class ConfiancaTests
{
    [Fact]
    public async Task AvaliacaoDenunciaEBloqueioAplicamRegrasDePrivacidade()
    {
        using var factory = await TestApi.CreateAsync();
        using var cliente = factory.CreateClient();
        using var prestador = factory.CreateClient();
        var clienteId = await AutenticarAsync(cliente);
        var prestadorId = await AutenticarAsync(prestador);
        int ofertaId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoriaId = await db.Categorias.Select(c => c.Id).FirstAsync();
            var oferta = new Oferta { PrestadorId = prestadorId, CategoriaId = categoriaId, Titulo = "Serviço avaliado", Descricao = "Descrição suficiente para a oferta de integração do serviço.", Status = StatusOferta.Ativo };
            db.Ofertas.Add(oferta);
            await db.SaveChangesAsync();
            ofertaId = oferta.Id;
        }
        using var solicitacaoResposta = await cliente.PostAsJsonAsync($"/api/ofertas/{ofertaId}/solicitacoes", new CriarSolicitacaoRequest("Preciso fazer este serviço com urgência."));
        solicitacaoResposta.EnsureSuccessStatusCode();
        var solicitacao = (await solicitacaoResposta.Content.ReadFromJsonAsync<SolicitacaoDto>())!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var item = await db.Solicitacoes.SingleAsync(s => s.Id == solicitacao.Id);
            item.Etapa = EtapaKanban.Concluidos;
            item.ConcluidoPorCliente = item.ConcluidoPorPrestador = true;
            await db.SaveChangesAsync();
        }

        using var avaliacaoResposta = await cliente.PostAsJsonAsync($"/api/solicitacoes/{solicitacao.Id}/avaliacoes", new AvaliarRequest(5, "Serviço bem executado."));
        avaliacaoResposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var avaliacao = (await avaliacaoResposta.Content.ReadFromJsonAsync<AvaliacaoDto>())!;
        (await cliente.GetFromJsonAsync<AvaliacaoDto>($"/api/solicitacoes/{solicitacao.Id}/avaliacoes/minha"))!.Nota.Should().Be(5);
        (await prestador.PostAsJsonAsync($"/api/avaliacoes/{avaliacao.Id}/resposta", new ResponderAvaliacaoRequest("Obrigado pela avaliação."))).StatusCode.Should().Be(HttpStatusCode.OK);
        var publica = await prestador.GetFromJsonAsync<AvaliacaoPublicaDto[]>($"/api/prestadores/{prestadorId}/avaliacoes");
        publica!.Should().ContainSingle(a => a.Nota == 5 && a.Avaliador == "Cliente do Gera" && a.Resposta == "Obrigado pela avaliação.");
        using var duplicada = await cliente.PostAsJsonAsync($"/api/solicitacoes/{solicitacao.Id}/avaliacoes", new AvaliarRequest(4, null));
        duplicada.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var denuncia = await cliente.PostAsJsonAsync("/api/denuncias", new DenunciarRequest(TipoAlvoDenuncia.Oferta, ofertaId, "Conteúdo enganoso", null));
        denuncia.StatusCode.Should().Be(HttpStatusCode.Accepted);
        using var duplicadaDenuncia = await cliente.PostAsJsonAsync("/api/denuncias", new DenunciarRequest(TipoAlvoDenuncia.Oferta, ofertaId, "Conteúdo enganoso", null));
        duplicadaDenuncia.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var bloqueio = await cliente.PutAsJsonAsync($"/api/bloqueios/{prestadorId}", new BloquearRequest("Não quero contato."));
        bloqueio.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await cliente.GetFromJsonAsync<BloqueioDto[]>("/api/bloqueios"))!.Should().ContainSingle(b => b.UsuarioId == prestadorId);
        using var novaSolicitacao = await cliente.PostAsJsonAsync($"/api/ofertas/{ofertaId}/solicitacoes", new CriarSolicitacaoRequest("Preciso fazer este serviço com urgência."));
        novaSolicitacao.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var historicoBloqueado = await cliente.GetAsync($"/api/mensagens/{solicitacao.Id}");
        historicoBloqueado.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var desbloqueio = await cliente.DeleteAsync($"/api/bloqueios/{prestadorId}");
        desbloqueio.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
