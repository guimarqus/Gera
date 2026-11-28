using Gera.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gera.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/prestadores")]
public sealed class PrestadoresController(PrestadorPublicoService prestadores) : ControllerBase
{
    [HttpGet("{id:int}")]
    [EnableRateLimiting("feed-ofertas")]
    public async Task<ActionResult<PrestadorPublicoDto>> Obter(int id, CancellationToken ct, int pagina = 1, int tamanho = 12)
    {
        if (id < 1 || pagina is < 1 or > 1_000_000 || tamanho is < 1 or > 50)
            return Problem(statusCode: 400, detail: "Identificador ou paginação inválida.");

        return Ok(await prestadores.ObterAsync(id, pagina, tamanho, ct));
    }
}
