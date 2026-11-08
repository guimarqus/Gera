using System.Security.Claims;
using Gera.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Gera.Api.Controllers;

[ApiController]
[Route("api/perfil")]
public sealed class PerfilController(IProfileSummaryService profileService) : ControllerBase
{
    [HttpGet("resumo")]
    public async Task<ActionResult<ProfileSummaryDto>> Resumo(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue("sub");
        if (!int.TryParse(subject, out var userId)) return Unauthorized();
        var summary = await profileService.GetAsync(userId, cancellationToken);
        return summary is null ? NotFound() : Ok(summary);
    }
}
