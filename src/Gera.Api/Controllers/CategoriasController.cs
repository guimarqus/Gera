using Gera.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gera.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[AllowAnonymous]
public sealed class CategoriasController(ICategoriasService categoriasService) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IReadOnlyList<CategoriaTreeDto>>> Get(CancellationToken cancellationToken)
        => Ok(await categoriasService.GetTreeAsync(cancellationToken));
}
