using Gera.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gera.Application.Catalog;

public sealed class CategoriasService(IAppDbContext db, IMemoryCache cache) : ICategoriasService
{
    private const string CacheKey = "gera:categorias:arvore:v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<IReadOnlyList<CategoriaTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<CategoriaTreeDto>? cached) && cached is not null)
            return cached;

        var categorias = await db.Categorias.AsNoTracking().Where(c => c.Ativo).ToListAsync(cancellationToken);
        var tree = CategoriaTreeBuilder.Build(categorias);
        cache.Set(CacheKey, tree, CacheDuration);
        return tree;
    }
}
