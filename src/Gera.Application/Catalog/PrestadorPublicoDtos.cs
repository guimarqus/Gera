namespace Gera.Application.Catalog;

public sealed record OfertaPrestadorDto(
    int Id, int CategoriaId, string Categoria, string Titulo, string Descricao,
    Gera.Domain.Enums.TipoPreco? TipoPreco, decimal? Valor, string? Cidade,
    string? Bairro, string? FotoCapaUrl, DateTime? PublicadoEm);

public sealed record PrestadorPublicoDto(
    int Id, string Nome, string? FotoUrl, string? Biografia, DateTime Desde,
    double? NotaMedia, int TotalAvaliacoes, int TotalOfertas, int Pagina,
    int Tamanho, int TotalPaginas, IReadOnlyList<OfertaPrestadorDto> Ofertas);
