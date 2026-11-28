using FluentValidation;

namespace Gera.Application.Ofertas;

public sealed class OfertaFeedFiltro
{
    public string? Busca { get; init; }
    public int? CategoriaId { get; init; }
    public decimal? PrecoMinimo { get; init; }
    public decimal? PrecoMaximo { get; init; }
    public string? Disponibilidade { get; init; }
    public string? Cidade { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public int RaioKm { get; init; } = 25;
    public string Ordem { get; init; } = "proximidade";
    public int Pagina { get; init; } = 1;
    public int Tamanho { get; init; } = 20;
}

public sealed record OfertaFeedDto(
    int Id, int? CategoriaId, string Categoria, string Titulo, string Descricao,
    Gera.Domain.Enums.TipoPreco? TipoPreco, decimal? Valor, string? Cidade, string? Bairro,
    string? Disponibilidade, string? FotoCapaUrl, int PrestadorId, string Prestador, string? PrestadorFotoUrl,
    double? NotaMedia, int TotalAvaliacoes, double? DistanciaKm, bool EhFavorita);

public sealed record OfertaFeedPaginaDto(
    IReadOnlyList<OfertaFeedDto> Ofertas, int Pagina, int Tamanho, int Total, int TotalPaginas);

public sealed class OfertaFeedFiltroValidator : AbstractValidator<OfertaFeedFiltro>
{
    private static readonly string[] Ordens = ["proximidade", "recentes", "menor-preco", "maior-preco", "melhor-avaliadas"];

    public OfertaFeedFiltroValidator()
    {
        RuleFor(f => f.Busca).MaximumLength(100);
        RuleFor(f => f.Cidade).MaximumLength(120);
        RuleFor(f => f.Disponibilidade).MaximumLength(200);
        RuleFor(f => f.CategoriaId).GreaterThan(0);
        RuleFor(f => f.PrecoMinimo).InclusiveBetween(0, 9999999999999999.99m);
        RuleFor(f => f.PrecoMaximo).InclusiveBetween(0, 9999999999999999.99m);
        RuleFor(f => f).Must(f => !f.PrecoMinimo.HasValue || !f.PrecoMaximo.HasValue || f.PrecoMinimo <= f.PrecoMaximo)
            .WithMessage("O preço mínimo não pode ser maior que o máximo.");
        RuleFor(f => f.Latitude).Must(value => !value.HasValue ||
            (double.IsFinite(value.Value) && value.Value is >= -90 and <= 90));
        RuleFor(f => f.Longitude).Must(value => !value.HasValue ||
            (double.IsFinite(value.Value) && value.Value is >= -180 and <= 180));
        RuleFor(f => f).Must(f => f.Latitude.HasValue == f.Longitude.HasValue)
            .WithMessage("Informe latitude e longitude juntas.");
        RuleFor(f => f.RaioKm).InclusiveBetween(1, 200);
        RuleFor(f => f.Ordem).Must(ordem => Ordens.Contains(ordem, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Selecione uma ordenação válida.");
        RuleFor(f => f.Pagina).InclusiveBetween(1, 1_000_000);
        RuleFor(f => f.Tamanho).InclusiveBetween(1, 50);
    }
}
