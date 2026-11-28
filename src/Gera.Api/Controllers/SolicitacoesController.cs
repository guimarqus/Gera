using System.Security.Claims;
using Gera.Application.Solicitacoes;
using Gera.Api.Hubs;
using Gera.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/solicitacoes")]
public sealed class SolicitacoesController(SolicitacaoService solicitacoes, IHubContext<ChatHub> hub) : ControllerBase
{
    private int? UsuarioId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpPost("/api/ofertas/{ofertaId:int}/solicitacoes")]
    public async Task<ActionResult<SolicitacaoDto>> Criar(int ofertaId, CriarSolicitacaoRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        var item = await solicitacoes.CriarAsync(userId, ofertaId, request, ct);
        await PublicarAsync(item, ct);
        return CreatedAtAction(nameof(Obter), new { id = item.Id }, item);
    }

    [HttpGet("minhas")]
    public async Task<ActionResult<IReadOnlyList<SolicitacaoDto>>> Minhas([FromQuery] EtapaKanban? etapa, CancellationToken ct) =>
        UsuarioId is int userId ? Ok(await solicitacoes.MinhasAsync(userId, etapa, ct)) : Unauthorized();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitacaoDto>> Obter(int id, CancellationToken ct) =>
        UsuarioId is int userId ? Ok(await solicitacoes.ObterAsync(userId, id, ct)) : Unauthorized();

    [HttpPost("{id:int}/orcamentos")]
    public async Task<ActionResult<SolicitacaoDto>> Propor(int id, ProporOrcamentoRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        var item = await solicitacoes.ProporAsync(userId, id, request, ct);
        return Ok(await PublicarAsync(item, ct));
    }

    [HttpPost("{id:int}/orcamentos/{orcamentoId:int}/aceitar")]
    public async Task<ActionResult<SolicitacaoDto>> Aceitar(int id, int orcamentoId, ResponderOrcamentoRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        return Ok(await PublicarAsync(await solicitacoes.ResponderOrcamentoAsync(userId, id, orcamentoId, true, request.Versao, ct), ct));
    }

    [HttpPost("{id:int}/orcamentos/{orcamentoId:int}/recusar")]
    public async Task<ActionResult<SolicitacaoDto>> Recusar(int id, int orcamentoId, ResponderOrcamentoRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        return Ok(await PublicarAsync(await solicitacoes.ResponderOrcamentoAsync(userId, id, orcamentoId, false, request.Versao, ct), ct));
    }

    [HttpPatch("{id:int}/etapa")]
    public async Task<ActionResult<SolicitacaoDto>> Mover(int id, MoverSolicitacaoRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        return Ok(await PublicarAsync(await solicitacoes.MoverAsync(userId, id, request, ct), ct));
    }

    [HttpPost("{id:int}/solicitar-conclusao")]
    public async Task<ActionResult<SolicitacaoDto>> SolicitarConclusao(int id, AcaoSolicitacaoRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        return Ok(await PublicarAsync(await solicitacoes.SolicitarConclusaoAsync(userId, id, request, ct), ct));
    }

    [HttpPost("{id:int}/confirmar-conclusao")]
    public async Task<ActionResult<SolicitacaoDto>> ConfirmarConclusao(int id, AcaoSolicitacaoRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        return Ok(await PublicarAsync(await solicitacoes.ConfirmarConclusaoAsync(userId, id, request, ct), ct));
    }

    private async Task<SolicitacaoDto> PublicarAsync(SolicitacaoDto item, CancellationToken ct)
    {
        await hub.Clients.Group(ChatHub.GrupoSolicitacao(item.Id)).SendAsync("SolicitacaoAtualizada", item, ct);
        if (UsuarioId is int actorId)
        {
            var destinatario = actorId == item.ClienteId ? item.PrestadorId : item.ClienteId;
            await hub.Clients.Group(ChatHub.GrupoUsuario(destinatario)).SendAsync("NotificacaoRecebida", ct);
        }
        return item;
    }

}
