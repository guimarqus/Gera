using System.Security.Claims;
using Gera.Api.Hubs;
using Gera.Application.Mensagens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mensagens")]
public sealed class MensagensController(MensagemService mensagens, IHubContext<ChatHub> hub) : ControllerBase
{
    private int? UsuarioId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet("conversas")]
    public async Task<ActionResult<IReadOnlyList<ConversaDto>>> Conversas(CancellationToken ct) =>
        UsuarioId is int id ? Ok(await mensagens.ConversasAsync(id, ct)) : Unauthorized();

    [HttpGet("{solicitacaoId:int}")]
    public async Task<ActionResult<IReadOnlyList<MensagemDto>>> Historico(int solicitacaoId, [FromQuery] long? antesDe, CancellationToken ct) =>
        UsuarioId is int id ? Ok(await mensagens.HistoricoAsync(id, solicitacaoId, antesDe, ct)) : Unauthorized();

    [HttpPost("{solicitacaoId:int}")]
    [EnableRateLimiting("chat-mensagens")]
    public async Task<ActionResult<MensagemDto>> Enviar(int solicitacaoId, EnviarMensagemRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int id) return Unauthorized();
        var message = await mensagens.EnviarTextoAsync(id, solicitacaoId, request.Conteudo, ct);
        await hub.Clients.Group(ChatHub.GrupoSolicitacao(solicitacaoId)).SendAsync("NovaMensagem", message, ct);
        await NotificarRealtimeAsync(solicitacaoId, id, ct);
        return Ok(message);
    }

    [HttpPost("{solicitacaoId:int}/midia")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024 + 64 * 1024)]
    [EnableRateLimiting("midia-chat")]
    public async Task<ActionResult<MensagemDto>> EnviarMidia(int solicitacaoId, IFormFile arquivo, CancellationToken ct)
    {
        if (UsuarioId is not int id) return Unauthorized();
        if (arquivo is null) return BadRequest(new { detalhe = "Selecione uma imagem ou áudio." });
        await using var conteudo = arquivo.OpenReadStream();
        var message = await mensagens.EnviarMidiaAsync(id, solicitacaoId, conteudo, arquivo.Length, ct);
        await hub.Clients.Group(ChatHub.GrupoSolicitacao(solicitacaoId)).SendAsync("NovaMensagem", message, ct);
        await NotificarRealtimeAsync(solicitacaoId, id, ct);
        return Ok(message);
    }

    [HttpPost("{solicitacaoId:int}/lidas")]
    public async Task<IActionResult> MarcarLidas(int solicitacaoId, CancellationToken ct)
    {
        if (UsuarioId is not int id) return Unauthorized();
        var count = await mensagens.MarcarLidasAsync(id, solicitacaoId, ct);
        await hub.Clients.Group(ChatHub.GrupoSolicitacao(solicitacaoId)).SendAsync("MensagensLidas", id, ct);
        return Ok(new { marcadas = count });
    }

    [HttpGet("midia/{id}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> AbrirMidia(string id, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        if (!Guid.TryParseExact(id, "N", out var key)) return NotFound();
        var (arquivo, contentType) = await mensagens.AbrirMidiaAsync(userId, key, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(arquivo, contentType, enableRangeProcessing: contentType.StartsWith("audio/", StringComparison.Ordinal));
    }

    private async Task NotificarRealtimeAsync(int solicitacaoId, int remetenteId, CancellationToken ct)
    {
        var destinatario = await mensagens.OutroParticipanteAsync(remetenteId, solicitacaoId, ct);
        await hub.Clients.Group(ChatHub.GrupoUsuario(destinatario)).SendAsync("NotificacaoRecebida", ct);
    }
}
