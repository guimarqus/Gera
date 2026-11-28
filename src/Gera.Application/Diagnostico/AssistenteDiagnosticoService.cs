using Gera.Application.Interfaces;
using Gera.Application.Ofertas;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Diagnostico;

public sealed class AssistenteDiagnosticoService(
    IAppDbContext db,
    IAssistenteDiagnosticoProvider provider,
    OfertaFeedService feed)
{
    private const string AvisoSeguranca = "Se houver cheiro de gás, fumaça, faíscas, risco de choque ou estrutura instável, afaste-se e procure os serviços de emergência e um profissional habilitado.";

    public async Task<DiagnosticoResponse> AnalisarAsync(DiagnosticoRequest request, int? usuarioId, CancellationToken ct)
    {
        var problema = string.Join(' ', request.Problema.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        var categoriasDisponiveis = await db.Categorias.AsNoTracking().Where(categoria => categoria.Ativo)
            .OrderBy(categoria => categoria.Nome).Select(categoria => new CategoriaDisponivel(categoria.Id, categoria.Nome))
            .Take(100).ToListAsync(ct);
        var analise = await provider.AnalisarAsync(problema, categoriasDisponiveis, ct);
        var categoriasPorId = categoriasDisponiveis.ToDictionary(categoria => categoria.Id);
        var categorias = analise.CategoriaIds.Distinct().Where(categoriasPorId.ContainsKey).Take(3)
            .Select(id => categoriasPorId[id]).ToArray();
        var resultados = new List<Ofertas.OfertaFeedDto>();
        foreach (var categoria in categorias)
        {
            var pagina = await feed.PesquisarAsync(new OfertaFeedFiltro
            {
                CategoriaId = categoria.Id,
                Cidade = string.IsNullOrWhiteSpace(request.Cidade) ? null : request.Cidade.Trim(),
                Ordem = "melhor-avaliadas",
                Pagina = 1,
                Tamanho = 5
            }, usuarioId, ct);
            resultados.AddRange(pagina.Ofertas);
        }

        var cautela = RequerCautela(problema, categorias.Select(categoria => categoria.Nome));
        return new DiagnosticoResponse(
            analise.Resumo,
            categorias.Select(categoria => new CategoriaDiagnosticoDto(categoria.Id, categoria.Nome)).ToArray(),
            analise.SequenciaSugerida.Take(3).ToArray(),
            cautela,
            cautela ? AvisoSeguranca : null,
            resultados.DistinctBy(oferta => oferta.Id).Take(8).ToArray());
    }

    private static bool RequerCautela(string problema, IEnumerable<string> categorias)
    {
        var texto = $"{problema} {string.Join(' ', categorias)}";
        return new[] { "el[eé]tric", "choque", "fio", "tomada", "curto", "g[aá]s", "vazamento de g[aá]s", "estrutura", "rachadura", "trinca", "desabamento", "telhado" }
            .Any(padrao => System.Text.RegularExpressions.Regex.IsMatch(texto, padrao, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant));
    }
}
