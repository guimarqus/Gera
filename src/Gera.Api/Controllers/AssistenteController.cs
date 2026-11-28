using System.Security.Claims;
using Gera.Application.Diagnostico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gera.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assistente")]
public sealed class AssistenteController(
    AssistenteDiagnosticoService assistente,
    IAssistenteDiagnosticoProvider provider) : ControllerBase
{
    [HttpPost("diagnostico")]
    [RequestSizeLimit(16_384)]
    [EnableRateLimiting("assistente-diagnostico")]
    public async Task<ActionResult<DiagnosticoResponse>> Diagnosticar(DiagnosticoRequest request, CancellationToken ct)
    {
        if (!provider.EstaConfigurado)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "O assistente de diagnóstico ainda não está configurado.");
        int? usuarioId = int.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;
        return Ok(await assistente.AnalisarAsync(request, usuarioId, ct));
    }
}
