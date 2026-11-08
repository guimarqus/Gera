namespace Gera.Application.Catalog;

public sealed record CategoriaTreeDto(
    int Id,
    string Nome,
    string? Descricao,
    string? Icone,
    IReadOnlyList<CategoriaTreeDto> Subcategorias);

public sealed record ProfileSummaryDto(
    string Nome,
    string? FotoUrl,
    IReadOnlyList<string> Papeis,
    string PapelAtivo);
