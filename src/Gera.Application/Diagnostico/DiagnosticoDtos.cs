using FluentValidation;

namespace Gera.Application.Diagnostico;

public sealed record DiagnosticoRequest(string Problema, string? Cidade, bool ConsentiuCompartilhamento);
public sealed record CategoriaDiagnosticoDto(int Id, string Nome);
public sealed record DiagnosticoResponse(
    string Resumo,
    IReadOnlyList<CategoriaDiagnosticoDto> Categorias,
    IReadOnlyList<string> SequenciaSugerida,
    bool RequerCautela,
    string? Aviso,
    IReadOnlyList<Ofertas.OfertaFeedDto> Ofertas);

public sealed class DiagnosticoRequestValidator : AbstractValidator<DiagnosticoRequest>
{
    public DiagnosticoRequestValidator()
    {
        RuleFor(request => request.Problema).NotEmpty().MinimumLength(8).MaximumLength(2000);
        RuleFor(request => request.Cidade).MaximumLength(120);
        RuleFor(request => request.ConsentiuCompartilhamento).Equal(true)
            .WithMessage("Confirme o compartilhamento da descrição com o serviço de IA para continuar.");
    }
}

public sealed record CategoriaDisponivel(int Id, string Nome);
public sealed record AnaliseDiagnostico(string Resumo, IReadOnlyList<int> CategoriaIds, IReadOnlyList<string> SequenciaSugerida);

public interface IAssistenteDiagnosticoProvider
{
    bool EstaConfigurado { get; }
    Task<AnaliseDiagnostico> AnalisarAsync(string problema, IReadOnlyList<CategoriaDisponivel> categorias, CancellationToken cancellationToken);
}
