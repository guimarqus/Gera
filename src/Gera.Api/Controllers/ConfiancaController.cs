using System.Security.Claims;
using Gera.Application.Confianca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class ConfiancaController(ConfiancaService confianca) : ControllerBase
{
    private int? UsuarioId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpPost("solicitacoes/{id:int}/avaliacoes")]
    [EnableRateLimiting("chat-mensagens")]
    public async Task<ActionResult<AvaliacaoDto>> Avaliar(int id, AvaliarRequest request, CancellationToken ct) =>
        UsuarioId is int usuarioId ? Ok(await confianca.AvaliarAsync(usuarioId, id, request, ct)) : Unauthorized();

    [HttpGet("solicitacoes/{id:int}/avaliacoes/minha")]
    public async Task<ActionResult<AvaliacaoDto?>> MinhaAvaliacao(int id, CancellationToken ct) =>
        UsuarioId is int usuarioId ? Ok(await confianca.MinhaAvaliacaoAsync(usuarioId, id, ct)) : Unauthorized();

    [HttpPost("avaliacoes/{id:int}/resposta")]
    [EnableRateLimiting("chat-mensagens")]
    public async Task<ActionResult<AvaliacaoDto>> Responder(int id, ResponderAvaliacaoRequest request, CancellationToken ct) =>
        UsuarioId is int usuarioId ? Ok(await confianca.ResponderAsync(usuarioId, id, request, ct)) : Unauthorized();

    [HttpGet("prestadores/{id:int}/avaliacoes")]
    [AllowAnonymous]
    [EnableRateLimiting("feed-ofertas")]
    public async Task<ActionResult<IReadOnlyList<AvaliacaoPublicaDto>>> Avaliacoes(int id, int pagina = 1, int tamanho = 10, CancellationToken ct = default) =>
        Ok(await confianca.AvaliacoesPublicasAsync(id, pagina, tamanho, ct));

    [HttpPost("denuncias")]
    [EnableRateLimiting("chat-mensagens")]
    public async Task<IActionResult> Denunciar(DenunciarRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        await confianca.DenunciarAsync(usuarioId, request, ct);
        return Accepted();
    }

    [HttpGet("bloqueios")]
    public async Task<ActionResult<IReadOnlyList<BloqueioDto>>> Bloqueios(CancellationToken ct) =>
        UsuarioId is int usuarioId ? Ok(await confianca.BloqueiosAsync(usuarioId, ct)) : Unauthorized();

    [HttpPut("bloqueios/{usuarioAlvoId:int}")]
    public async Task<IActionResult> Bloquear(int usuarioAlvoId, BloquearRequest request, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        await confianca.BloquearAsync(usuarioId, usuarioAlvoId, request.Motivo, ct);
        return NoContent();
    }

    [HttpDelete("bloqueios/{usuarioAlvoId:int}")]
    public async Task<IActionResult> Desbloquear(int usuarioAlvoId, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        await confianca.DesbloquearAsync(usuarioId, usuarioAlvoId, ct);
        return NoContent();
    }
}
