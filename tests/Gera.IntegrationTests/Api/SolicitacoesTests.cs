using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gera.Application.Auth;
using Gera.Application.Solicitacoes;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gera.IntegrationTests.Api;

public sealed class SolicitacoesTests
{
    [Fact]
    public async Task SolicitarNegociarAgendarExecutarEConfirmarConclusao()
    {
        using var factory = await TestApi.CreateAsync();
        using var cliente = factory.CreateClient();
        using var prestador = factory.CreateClient();
        using var terceiro = factory.CreateClient();
        var clienteId = await AutenticarAsync(cliente);
        var prestadorId = await AutenticarAsync(prestador);
        await AutenticarAsync(terceiro);
        int ofertaId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoriaId = await db.Categorias.Select(c => c.Id).FirstAsync();
            var oferta = new Oferta { PrestadorId = prestadorId, CategoriaId = categoriaId, Titulo = "Serviço de teste", Descricao = "Descrição de oferta para integração.", Status = StatusOferta.Ativo };
            db.Ofertas.Add(oferta);
            await db.SaveChangesAsync();
            ofertaId = oferta.Id;
        }

        using var criada = await cliente.PostAsJsonAsync($"/api/ofertas/{ofertaId}/solicitacoes", new CriarSolicitacaoRequest("Preciso deste serviço em minha residência."));
        criada.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = (await criada.Content.ReadFromJsonAsync<SolicitacaoDto>())!;
        item.Etapa.Should().Be(EtapaKanban.Solicitacoes);
        var versaoInicial = item.Versao;
        (await terceiro.GetAsync($"/api/solicitacoes/{item.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Notificacoes.AnyAsync(n => n.ReferenciaId == item.Id && n.UsuarioId == prestadorId && n.Tipo == TipoNotificacao.NovoPedido)).Should().BeTrue();
        }

        using var proposta = await prestador.PostAsJsonAsync($"/api/solicitacoes/{item.Id}/orcamentos", new ProporOrcamentoRequest(250, 2, DateTime.UtcNow.AddDays(4), "Mão de obra e acabamento", null, item.Versao));
        proposta.EnsureSuccessStatusCode();
        item = (await proposta.Content.ReadFromJsonAsync<SolicitacaoDto>())!;
        item.Etapa.Should().Be(EtapaKanban.EmNegociacao);
        var quote = item.Orcamentos.Single();

        using var versaoDesatualizada = await prestador.PostAsJsonAsync($"/api/solicitacoes/{item.Id}/orcamentos", new ProporOrcamentoRequest(275, null, DateTime.UtcNow.AddDays(5), "Contraproposta", null, versaoInicial));
        versaoDesatualizada.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var aceito = await cliente.PostAsJsonAsync($"/api/solicitacoes/{item.Id}/orcamentos/{quote.Id}/aceitar", new ResponderOrcamentoRequest(item.Versao));
        aceito.EnsureSuccessStatusCode();
        item = (await aceito.Content.ReadFromJsonAsync<SolicitacaoDto>())!;
        item.Etapa.Should().Be(EtapaKanban.Agendados);
        item.Valor.Should().Be(250);

        using var tentativaCliente = await cliente.PatchAsJsonAsync($"/api/solicitacoes/{item.Id}/etapa", new MoverSolicitacaoRequest(EtapaKanban.EmExecucao, item.Versao));
        tentativaCliente.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var inicio = await prestador.PatchAsJsonAsync($"/api/solicitacoes/{item.Id}/etapa", new MoverSolicitacaoRequest(EtapaKanban.EmExecucao, item.Versao));
        inicio.EnsureSuccessStatusCode();
        item = (await inicio.Content.ReadFromJsonAsync<SolicitacaoDto>())!;

        using var pedidoConclusao = await prestador.PostAsJsonAsync($"/api/solicitacoes/{item.Id}/solicitar-conclusao", new AcaoSolicitacaoRequest(item.Versao));
        pedidoConclusao.EnsureSuccessStatusCode();
        item = (await pedidoConclusao.Content.ReadFromJsonAsync<SolicitacaoDto>())!;
        item.ConcluidoPorPrestador.Should().BeTrue();
        using var confirma = await cliente.PostAsJsonAsync($"/api/solicitacoes/{item.Id}/confirmar-conclusao", new AcaoSolicitacaoRequest(item.Versao));
        confirma.EnsureSuccessStatusCode();
        item = (await confirma.Content.ReadFromJsonAsync<SolicitacaoDto>())!;
        item.Etapa.Should().Be(EtapaKanban.Concluidos);
        item.Historicos.Select(h => h.EtapaNova).Should().ContainInOrder(EtapaKanban.EmNegociacao, EtapaKanban.Agendados, EtapaKanban.EmExecucao, EtapaKanban.Concluidos);
        item.Historicos.Should().OnlyContain(h => h.UsuarioId == clienteId || h.UsuarioId == prestadorId);
    }

    private static async Task<int> AutenticarAsync(HttpClient client)
    {
        var cadastro = TestApi.NewRegistration(Guid.NewGuid().ToString("N")[..10]);
        using var resposta = await client.PostAsJsonAsync("/api/auth/register", cadastro);
        resposta.EnsureSuccessStatusCode();
        var sessao = (await resposta.Content.ReadFromJsonAsync<PublicAuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.AccessToken);
        var usuario = await client.GetFromJsonAsync<UserResponse>("/api/auth/eu");
        return usuario!.Id;
    }
}
