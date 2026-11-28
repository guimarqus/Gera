using FluentValidation;
using Gera.Domain.Enums;

namespace Gera.Application.Ofertas;

public sealed class OfertaOptions
{
    public int LimiteAtivasPorUsuario { get; set; } = 20;
}

public sealed record AlterarStatusOfertaRequest(StatusOferta Status);

public sealed class AlterarStatusOfertaValidator : AbstractValidator<AlterarStatusOfertaRequest>
{
    public AlterarStatusOfertaValidator() =>
        RuleFor(o => o.Status).Must(status => status is StatusOferta.Ativo or StatusOferta.Pausado)
            .WithMessage("Selecione Ativo ou Pausado.");
}

public sealed record EditarOfertaRequest(
    int? CategoriaId, string? Titulo, string? Descricao, TipoPreco? TipoPreco,
    decimal? Valor, double? Latitude, double? Longitude, int? RaioKm,
    string? Cidade, string? Bairro, string? Disponibilidade, string? FormasPagamento,
    string Versao, Dictionary<string, string>? Atributos = null, string[]? Tags = null,
    int? VersaoModelo = null);

public sealed record CaracteristicasOferta(int VersaoModelo, Dictionary<string, string> Atributos);

public sealed record OfertaPrivadaDto(
    int Id, int? CategoriaId, string Titulo, string Descricao, TipoPreco? TipoPreco,
    decimal? Valor, double? Latitude, double? Longitude, int RaioKm, string? Cidade,
    string? Bairro, string? Disponibilidade, string? FormasPagamento, StatusOferta Status,
    DateTime CriadoEm, DateTime? PublicadoEm, string Versao, CaracteristicasOferta? Caracteristicas,
    IReadOnlyList<string> Tags);

public sealed record OfertaPublicaDto(
    int Id, int? CategoriaId, string Titulo, string Descricao, TipoPreco? TipoPreco,
    decimal? Valor, string? Cidade, string? Bairro, string? Disponibilidade,
    CaracteristicasOferta? Caracteristicas, IReadOnlyList<string> Tags);

public sealed record FotoOfertaPublicaDto(string Url, string? UrlMiniatura, int Ordem, bool EhCapa);

public sealed record OfertaDetalhePublicoDto(
    int Id, int CategoriaId, string Categoria, string Titulo, string Descricao, TipoPreco? TipoPreco,
    decimal? Valor, string? Cidade, string? Bairro, int RaioKm, string? Disponibilidade,
    string? FormasPagamento, CaracteristicasOferta? Caracteristicas, IReadOnlyList<string> Tags,
    IReadOnlyList<FotoOfertaPublicaDto> Fotos, int PrestadorId, string Prestador,
    string? PrestadorFotoUrl, string? PrestadorBiografia, DateTime PrestadorDesde);

public sealed class EditarOfertaValidator : AbstractValidator<EditarOfertaRequest>
{
    public EditarOfertaValidator()
    {
        RuleFor(o => o.CategoriaId).GreaterThan(0);
        RuleFor(o => o.Titulo).MaximumLength(120);
        RuleFor(o => o.Descricao).MaximumLength(2000);
        RuleFor(o => o.TipoPreco).IsInEnum();
        RuleFor(o => o.Valor).InclusiveBetween(0.01m, 9999999999999999.99m).PrecisionScale(18, 2, true);
        RuleFor(o => o.Latitude).InclusiveBetween(-90, 90);
        RuleFor(o => o.Longitude).InclusiveBetween(-180, 180);
        RuleFor(o => o).Must(o => o.Latitude.HasValue == o.Longitude.HasValue)
            .WithMessage("Informe latitude e longitude juntas.");
        RuleFor(o => o.RaioKm).InclusiveBetween(1, 100);
        RuleFor(o => o.Cidade).MaximumLength(120);
        RuleFor(o => o.Bairro).MaximumLength(120);
        RuleFor(o => o.Disponibilidade).MaximumLength(200);
        RuleFor(o => o.FormasPagamento).MaximumLength(200);
        RuleFor(o => o.Versao).NotEmpty().Length(12);
        RuleFor(o => o.Tags).Must(tags => tags is null || tags.Length <= 5)
            .WithMessage("Informe no máximo cinco tags.");
        RuleForEach(o => o.Tags).NotEmpty().MaximumLength(50);
        RuleFor(o => o.Atributos).Must(atributos => atributos is null || atributos.Count <= 12)
            .WithMessage("O modelo aceita no máximo doze características.");
    }
}
