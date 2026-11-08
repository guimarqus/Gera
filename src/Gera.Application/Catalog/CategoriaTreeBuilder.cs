using Gera.Domain.Entities;

namespace Gera.Application.Catalog;

public static class CategoriaTreeBuilder
{
    public static IReadOnlyList<CategoriaTreeDto> Build(IEnumerable<Categoria> categorias)
    {
        var active = categorias.Where(c => c.Ativo).ToList();
        var grouped = active.GroupBy(c => c.CategoriaPaiId ?? 0)
            .ToDictionary(group => group.Key, group => group.OrderBy(c => c.Ordem).ThenBy(c => c.Nome).ToArray());
        return BuildLevel(null);

        IReadOnlyList<CategoriaTreeDto> BuildLevel(int? parentId)
        {
            if (!grouped.TryGetValue(parentId ?? 0, out var items)) return [];
            return items.Select(c => new CategoriaTreeDto(
                c.Id, c.Nome, c.Descricao, c.Icone, BuildLevel(c.Id))).ToArray();
        }
    }
}
