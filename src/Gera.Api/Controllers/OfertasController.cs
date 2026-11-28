using System.Security.Claims;
using Gera.Application.Ofertas;
using Gera.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ofertas")]
public sealed class OfertasController(OfertaService ofertas, OfertaFeedService feed) : ControllerBase
{
    private int? UsuarioId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [AllowAnonymous]
    [HttpGet("feed")]
    [EnableRateLimiting("feed-ofertas")]
    public async Task<ActionResult<OfertaFeedPaginaDto>> Feed([FromQuery] OfertaFeedFiltro filtros, CancellationToken ct) =>
        Ok(await feed.PesquisarAsync(filtros, UsuarioId, ct));

    [AllowAnonymous]
    [HttpGet("{id:int}/detalhes")]
    [EnableRateLimiting("feed-ofertas")]
    public async Task<ActionResult<OfertaDetalhePublicoDto>> DetalhesPublicos(int id, CancellationToken ct) =>
        Ok(await ofertas.ObterDetalhesPublicosAsync(id, ct));

    [HttpPut("{id:int}/favorito")]
    public async Task<IActionResult> Favoritar(int id, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        await feed.FavoritarAsync(id, usuarioId, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/favorito")]
    public async Task<IActionResult> Desfavoritar(int id, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        await feed.DesfavoritarAsync(id, usuarioId, ct);
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<OfertaPrivadaDto>> Criar(CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        var oferta = await ofertas.CriarAsync(usuarioId, ct);
        return CreatedAtAction(nameof(Obter), new { id = oferta.Id }, oferta);
    }

    [HttpGet("minhas")]
    public async Task<ActionResult<IReadOnlyList<OfertaPrivadaDto>>> Minhas(
        CancellationToken ct, int pagina = 1, int tamanho = 20, StatusOferta? status = null)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        if (pagina is < 1 or > 1000000 || tamanho is < 1 or > 100 ||
            (status.HasValue && !Enum.IsDefined(status.Value)))
            return Problem(statusCode: 400, detail: "Paginação ou status inválido.");

        return Ok(await ofertas.ListarAsync(usuarioId, pagina, tamanho, status, ct));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct) =>
        Ok(await ofertas.ObterAsync(id, UsuarioId, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OfertaPrivadaDto>> Editar(int id, EditarOfertaRequest dados, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        return Ok(await ofertas.EditarAsync(id, usuarioId, dados, ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        await ofertas.ExcluirAsync(id, usuarioId, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/publicar")]
    public async Task<ActionResult<OfertaPrivadaDto>> Publicar(int id, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        return Ok(await ofertas.PublicarAsync(id, usuarioId, ct));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OfertaPrivadaDto>> AlterarStatus(int id, AlterarStatusOfertaRequest dados, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        return Ok(await ofertas.AlterarStatusAsync(id, usuarioId, dados.Status, ct));
    }
}
