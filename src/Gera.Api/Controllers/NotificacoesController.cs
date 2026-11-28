using System.Security.Claims;
using Gera.Application.Mensagens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificacoes")]
public sealed class NotificacoesController(NotificacaoService notificacoes) : ControllerBase
{
    private int? UsuarioId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet]
    public async Task<ActionResult<NotificacoesPaginaDto>> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanho = 20, CancellationToken ct = default) =>
        UsuarioId is int id ? Ok(await notificacoes.ListarAsync(id, pagina, tamanho, ct)) : Unauthorized();

    [HttpPatch("{id:int}/lida")]
    public async Task<IActionResult> MarcarLida(int id, CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        await notificacoes.MarcarLidaAsync(userId, id, ct);
        return NoContent();
    }

    [HttpPost("lidas")]
    public async Task<IActionResult> MarcarTodas(CancellationToken ct)
    {
        if (UsuarioId is not int userId) return Unauthorized();
        await notificacoes.MarcarTodasLidasAsync(userId, ct);
        return NoContent();
    }

    [HttpGet("preferencias")]
    public async Task<ActionResult<PreferenciasNotificacaoDto>> Preferencias(CancellationToken ct) =>
        UsuarioId is int id ? Ok(await notificacoes.PreferenciasAsync(id, ct)) : Unauthorized();

    [HttpPut("preferencias")]
    public async Task<ActionResult<PreferenciasNotificacaoDto>> AlterarPreferencias(PreferenciasNotificacaoDto request, CancellationToken ct) =>
        UsuarioId is int id ? Ok(await notificacoes.AlterarPreferenciasAsync(id, request.Ativas, ct)) : Unauthorized();
}
