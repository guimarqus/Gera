using Gera.Application.Catalog;
using Gera.Application.Ofertas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gera.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[AllowAnonymous]
public sealed class CategoriasController(ICategoriasService categoriasService, ModeloOfertaService modelos) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IReadOnlyList<CategoriaTreeDto>>> Get(CancellationToken cancellationToken)
        => Ok(await categoriasService.GetTreeAsync(cancellationToken));

    [HttpGet("{id:int}/modelo-oferta")]
    public async Task<ActionResult<ModeloOfertaDto>> ModeloOferta(int id, CancellationToken cancellationToken)
        => Ok(await modelos.ObterAsync(id, cancellationToken));
}
