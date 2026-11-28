using Gera.Application.Interfaces;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Catalog;

public sealed class PrestadorPublicoService(IAppDbContext db)
{
    public async Task<PrestadorPublicoDto> ObterAsync(int id, int pagina, int tamanho, CancellationToken ct)
    {
        var prestador = await db.Usuarios.AsNoTracking().Where(usuario => usuario.Id == id && usuario.Ativo &&
            usuario.Ofertas.Any(oferta => oferta.Status == StatusOferta.Ativo && oferta.ExcluidoEm == null &&
                oferta.Categoria != null && oferta.Categoria.Ativo))
            .Select(usuario => new { usuario.Id, usuario.Nome, usuario.FotoUrl, usuario.Biografia, usuario.CriadoEm })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Prestador não encontrado.");

        var avaliacoes = await db.Avaliacoes.AsNoTracking().Where(avaliacao => avaliacao.AvaliadoId == id)
            .GroupBy(avaliacao => avaliacao.AvaliadoId)
            .Select(grupo => new { Media = (double?)grupo.Average(avaliacao => (double)avaliacao.Nota), Total = grupo.Count() })
            .SingleOrDefaultAsync(ct);

        var ofertas = db.Ofertas.AsNoTracking().Where(oferta => oferta.PrestadorId == id &&
            oferta.Status == StatusOferta.Ativo && oferta.ExcluidoEm == null &&
            oferta.Categoria != null && oferta.Categoria.Ativo);
        var totalOfertas = await ofertas.CountAsync(ct);
        var itens = await ofertas.OrderByDescending(oferta => oferta.PublicadoEm)
            .ThenByDescending(oferta => oferta.Id).Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(oferta => new OfertaPrestadorDto(
                oferta.Id, oferta.CategoriaId!.Value, oferta.Categoria!.Nome, oferta.Titulo,
                oferta.Descricao, oferta.TipoPreco, oferta.Valor, oferta.Cidade, oferta.Bairro,
                oferta.Fotos.OrderByDescending(foto => foto.EhCapa).ThenBy(foto => foto.Ordem)
                    .Select(foto => foto.UrlMiniatura ?? foto.Url).FirstOrDefault(), oferta.PublicadoEm))
            .ToListAsync(ct);

        return new PrestadorPublicoDto(prestador.Id, prestador.Nome, prestador.FotoUrl,
            prestador.Biografia, prestador.CriadoEm, avaliacoes?.Media, avaliacoes?.Total ?? 0,
            totalOfertas, pagina, tamanho, (int)Math.Ceiling(totalOfertas / (double)tamanho), itens);
    }
}
