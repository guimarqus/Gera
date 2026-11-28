namespace Gera.Application.Catalog;

public interface ICategoriasService
{
    Task<IReadOnlyList<CategoriaTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    void InvalidarCache();
}

public interface IProfileSummaryService
{
    Task<ProfileSummaryDto?> GetAsync(int usuarioId, CancellationToken cancellationToken = default);
}
