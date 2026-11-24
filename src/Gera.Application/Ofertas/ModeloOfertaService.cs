using Gera.Application.Interfaces;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Ofertas;

public sealed record CampoModeloOferta(string Chave, string Rotulo, string Tipo, string[]? Opcoes = null);
public sealed record ModeloOfertaDto(int CategoriaId, int Versao, IReadOnlyList<CampoModeloOferta> Campos);

public sealed class ModeloOfertaService(IAppDbContext db)
{
    public async Task<CaracteristicasOferta?> ValidarAsync(
        int? categoriaId, int? versao, Dictionary<string, string> atributos, CancellationToken cancellationToken)
    {
        if (atributos.Count == 0)
            return null;
        if (categoriaId is not int id)
            throw new DomainException("Selecione uma categoria antes de informar as características.");

        var modelo = await ObterAsync(id, cancellationToken);
        if (modelo.Versao != versao)
            throw new DomainException("O modelo da categoria mudou. Recarregue a oferta para continuar.");

        var valores = new Dictionary<string, string>();
        foreach (var (chave, valor) in atributos)
        {
            var campo = modelo.Campos.SingleOrDefault(c => c.Chave == chave);
            if (campo is null || valor is null)
                throw new DomainException("Há uma característica inválida para a categoria escolhida.");

            var texto = valor.Trim();
            if (texto.Length == 0)
                continue;
            if (texto.Length > 160 ||
                (campo.Tipo == "booleano" && texto is not ("true" or "false")) ||
                (campo.Opcoes is not null && !campo.Opcoes.Contains(texto, StringComparer.Ordinal)))
                throw new DomainException($"Confira o valor de {campo.Rotulo}.");
            valores.Add(chave, texto);
        }

        return valores.Count == 0 ? null : new CaracteristicasOferta(modelo.Versao, valores);
    }

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
