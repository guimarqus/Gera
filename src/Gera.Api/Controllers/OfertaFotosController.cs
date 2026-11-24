using System.Security.Claims;
using Gera.Application.Ofertas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ofertas/{id:int}/fotos")]
public sealed class OfertaFotosController(OfertaFotosService fotos) : ControllerBase
{
    private int? UsuarioId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(OfertaFotosService.MaximoBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = OfertaFotosService.MaximoBytes)]
    [EnableRateLimiting("fotos-ofertas")]
    public async Task<ActionResult<FotosOfertaDto>> Adicionar(int id, IFormFile arquivo, [FromForm] string versao, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        await using var conteudo = arquivo.OpenReadStream();
        return Ok(await fotos.AdicionarAsync(id, usuarioId, versao, conteudo, arquivo.Length, ct));
    }

    [AllowAnonymous]
    [HttpGet("/api/ofertas/{id:int}/midia/{nome}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Abrir(int id, string nome, CancellationToken ct)
    {
        var arquivo = await fotos.AbrirAsync(id, nome, UsuarioId, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(arquivo, "image/webp");
    }

    [HttpGet]
    public async Task<ActionResult<FotosOfertaDto>> Listar(int id, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        return Ok(await fotos.ListarAsync(id, usuarioId, ct));
    }

    [HttpPatch("ordem")]
    public async Task<ActionResult<FotosOfertaDto>> Ordenar(int id, OrdenarFotosRequest dados, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        return Ok(await fotos.OrdenarAsync(id, usuarioId, dados, ct));
    }

    [HttpPatch("{fotoId:int}/capa")]
    public async Task<ActionResult<FotosOfertaDto>> Capa(int id, int fotoId, AlterarFotoRequest dados, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        return Ok(await fotos.DefinirCapaAsync(id, fotoId, usuarioId, dados.Versao, ct));
    }

    [HttpDelete("{fotoId:int}")]
    public async Task<ActionResult<FotosOfertaDto>> Excluir(
        int id, int fotoId, [FromHeader(Name = "If-Match")] string versao, CancellationToken ct)
    {
        if (UsuarioId is not int usuarioId) return Unauthorized();
        return Ok(await fotos.ExcluirAsync(id, fotoId, usuarioId, versao.Trim('"'), ct));
    }
}
