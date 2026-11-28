using System.Security.Claims;
using Gera.Application.Admin;
using Gera.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize(Policy = "Administrador")]
[Route("api/admin")]
public sealed class AdminController(AdminService admin) : ControllerBase
{
    private int AdminId => int.TryParse(User.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();

    [HttpGet("resumo")]
    public async Task<ActionResult<AdminResumoDto>> Resumo(CancellationToken ct) => Ok(await admin.ResumoAsync(ct));

    [HttpGet("denuncias")]
    public async Task<ActionResult<IReadOnlyList<DenunciaAdminDto>>> Denuncias(StatusDenuncia? status, int pagina = 1, int tamanho = 20, CancellationToken ct = default) =>
        Ok(await admin.DenunciasAsync(status, pagina, tamanho, ct));

    [HttpPatch("denuncias/{id:int}")]
    public async Task<IActionResult> DecidirDenuncia(int id, DenunciaDecisaoRequest request, CancellationToken ct)
    {
        await admin.DecidirDenunciaAsync(AdminId, id, request, ct);
        return NoContent();
    }

    [HttpPatch("ofertas/{id:int}")]
    public async Task<IActionResult> ModerarOferta(int id, ModerarOfertaRequest request, CancellationToken ct)
    {
        await admin.ModerarOfertaAsync(AdminId, id, request, ct);
        return NoContent();
    }

    [HttpGet("categorias")]
    public async Task<ActionResult<IReadOnlyList<CategoriaAdminDto>>> Categorias(CancellationToken ct) => Ok(await admin.CategoriasAsync(ct));

    [HttpPost("categorias")]
    public async Task<ActionResult<CategoriaAdminDto>> CriarCategoria(CategoriaAdminRequest request, CancellationToken ct)
    {
        var categoria = await admin.CriarCategoriaAsync(AdminId, request, ct);
        return Created("/api/admin/categorias", categoria);
    }

    [HttpPut("categorias/{id:int}")]
    public async Task<ActionResult<CategoriaAdminDto>> EditarCategoria(int id, CategoriaAdminRequest request, CancellationToken ct) =>
        Ok(await admin.EditarCategoriaAsync(AdminId, id, request, ct));

    [HttpGet("usuarios")]
    public async Task<ActionResult<IReadOnlyList<UsuarioAdminDto>>> Usuarios(string? busca, int pagina = 1, int tamanho = 20, CancellationToken ct = default) =>
        Ok(await admin.UsuariosAsync(busca, pagina, tamanho, ct));

    [HttpPatch("usuarios/{id:int}/status")]
    public async Task<IActionResult> AlterarStatusUsuario(int id, StatusUsuarioRequest request, CancellationToken ct)
    {
        await admin.AlterarStatusUsuarioAsync(AdminId, id, request, ct);
        return NoContent();
    }

    [HttpPatch("usuarios/{id:int}/verificacao")]
    public async Task<IActionResult> AlterarVerificacao(int id, VerificarUsuarioRequest request, CancellationToken ct)
    {
        await admin.AlterarVerificacaoAsync(AdminId, id, request, ct);
        return NoContent();
    }

    [HttpGet("auditorias")]
    public async Task<ActionResult<IReadOnlyList<AuditoriaAdminDto>>> Auditorias(int pagina = 1, int tamanho = 50, CancellationToken ct = default) =>
        Ok(await admin.AuditoriasAsync(pagina, tamanho, ct));
}
