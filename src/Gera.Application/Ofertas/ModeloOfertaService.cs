using Gera.Application.Interfaces;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Ofertas;

public sealed record CampoModeloOferta(string Chave, string Rotulo, string Tipo, string[]? Opcoes = null);
public sealed record ModeloOfertaDto(int CategoriaId, int Versao, IReadOnlyList<CampoModeloOferta> Campos);

public sealed class ModeloOfertaService(IAppDbContext db)
{
    public async Task<ModeloOfertaDto> ObterAsync(int categoriaId, CancellationToken cancellationToken)
    {
        var categoria = await db.Categorias.AsNoTracking()
            .Where(c => c.Id == categoriaId && c.Ativo)
            .Select(c => new { c.Id, c.CategoriaPaiId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Categoria não encontrada.");

        CampoModeloOferta[] campos = (categoria.CategoriaPaiId ?? categoria.Id) switch
        {
            1 =>
            [
                new("tipoObra", "Tipo de obra", "selecao", ["Residencial", "Comercial", "Ambos"]),
                new("experiencia", "Experiência", "texto"),
                new("ajudante", "Trabalha com ajudante?", "booleano"),
                new("forneceMaterial", "Fornece material?", "booleano"),
                new("reformas", "Atende reformas?", "booleano"),
                new("servicos", "Serviços realizados", "texto")
            ],
            2 =>
            [
                new("tipoImovel", "Tipo de imóvel", "selecao", ["Residencial", "Comercial", "Ambos"]),
                new("servicos", "Serviços elétricos realizados", "texto"),
                new("emergencia", "Atende emergências?", "booleano")
            ],
            3 =>
            [
                new("servicos", "Serviços hidráulicos realizados", "texto"),
                new("emergencia", "Atende emergências?", "booleano")
            ],
            4 =>
            [
                new("ambiente", "Ambiente de pintura", "selecao", ["Interno", "Externo", "Ambos"]),
                new("acabamentos", "Tipos de acabamento", "texto"),
                new("forneceMaterial", "Fornece material?", "booleano")
            ],
            _ => [new("servicos", "Serviços realizados", "texto")]
        };

        return new ModeloOfertaDto(categoriaId, 1, campos);
    }
}
