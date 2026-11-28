using System.Data;
using Gera.Application.Interfaces;
using Gera.Domain;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Ofertas;

public sealed class OfertaFeedService(IAppDbContext db)
{
    public async Task<OfertaFeedPaginaDto> PesquisarAsync(OfertaFeedFiltro filtro, int? usuarioId, CancellationToken ct)
    {
        var localizacao = filtro.Latitude is double latitude && filtro.Longitude is double longitude
            ? GeoFactory.CriarPonto(latitude, longitude)
            : null;
        var query = db.Ofertas.AsNoTracking().Where(oferta =>
            oferta.Status == StatusOferta.Ativo && oferta.ExcluidoEm == null &&
            oferta.Categoria != null && oferta.Categoria.Ativo && oferta.Prestador.Ativo);

        if (filtro.CategoriaId is int categoriaId)
        {
            var categoriaIds = await db.Categorias.AsNoTracking()
                .Where(categoria => categoria.Id == categoriaId || categoria.CategoriaPaiId == categoriaId)
                .Select(categoria => categoria.Id).ToArrayAsync(ct);
            query = query.Where(oferta => oferta.CategoriaId.HasValue && categoriaIds.Contains(oferta.CategoriaId.Value));
        }
        if (filtro.PrecoMinimo is decimal minimo)
            query = query.Where(oferta => oferta.Valor >= minimo);
        if (filtro.PrecoMaximo is decimal maximo)
            query = query.Where(oferta => oferta.Valor <= maximo);
        if (!string.IsNullOrWhiteSpace(filtro.Disponibilidade))
        {
            var disponibilidade = filtro.Disponibilidade.Trim();
            query = query.Where(oferta => oferta.Disponibilidade != null && oferta.Disponibilidade.Contains(disponibilidade));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Cidade))
        {
            var cidade = filtro.Cidade.Trim();
            query = query.Where(oferta =>
                (oferta.Cidade != null && oferta.Cidade.Contains(cidade)) ||
                (oferta.Bairro != null && oferta.Bairro.Contains(cidade)));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            query = query.Where(oferta => oferta.Titulo.Contains(busca) || oferta.Descricao.Contains(busca) ||
                oferta.Tags.Any(tag => tag.Tag.Contains(busca)));
        }
        if (localizacao is not null)
        {
            var raioMetros = filtro.RaioKm * 1000d;
            query = query.Where(oferta => oferta.Localizacao != null &&
                oferta.Localizacao.Distance(localizacao) <= raioMetros &&
                oferta.Localizacao.Distance(localizacao) <= oferta.RaioKm * 1000d);
        }

        var total = await query.CountAsync(ct);
        var ordem = filtro.Ordem.ToLowerInvariant();
        var ordenadas = ordem switch
        {
            "menor-preco" => query.OrderBy(oferta => oferta.Valor == null).ThenBy(oferta => oferta.Valor).ThenByDescending(oferta => oferta.PublicadoEm),
            "maior-preco" => query.OrderBy(oferta => oferta.Valor == null).ThenByDescending(oferta => oferta.Valor).ThenByDescending(oferta => oferta.PublicadoEm),
            "melhor-avaliadas" => query.OrderByDescending(oferta => oferta.TotalAvaliacoes > 0)
                .ThenByDescending(oferta => oferta.NotaMedia).ThenByDescending(oferta => oferta.TotalAvaliacoes)
                .ThenByDescending(oferta => oferta.PublicadoEm),
            "recentes" => query.OrderByDescending(oferta => oferta.PublicadoEm),
            _ when localizacao is not null => query.OrderBy(oferta => oferta.Localizacao!.Distance(localizacao))
                .ThenByDescending(oferta => oferta.PublicadoEm),
            _ => query.OrderByDescending(oferta => oferta.PublicadoEm)
        };

        var ofertas = await ordenadas.Skip((filtro.Pagina - 1) * filtro.Tamanho).Take(filtro.Tamanho)
            .Select(oferta => new OfertaFeedDto(
                oferta.Id, oferta.CategoriaId, oferta.Categoria!.Nome, oferta.Titulo, oferta.Descricao,
                oferta.TipoPreco, oferta.Valor, oferta.Cidade, oferta.Bairro, oferta.Disponibilidade,
                oferta.Fotos.OrderByDescending(foto => foto.EhCapa).ThenBy(foto => foto.Ordem)
                    .Select(foto => foto.UrlMiniatura ?? foto.Url).FirstOrDefault(),
                oferta.Prestador.Nome, oferta.Prestador.FotoUrl,
                oferta.TotalAvaliacoes > 0 ? oferta.NotaMedia : null, oferta.TotalAvaliacoes,
                localizacao == null ? null : Math.Round(oferta.Localizacao!.Distance(localizacao) / 1000d, 1),
                usuarioId.HasValue && oferta.Favoritos.Any(favorito => favorito.UsuarioId == usuarioId.Value)))
            .ToListAsync(ct);

        return new OfertaFeedPaginaDto(ofertas, filtro.Pagina, filtro.Tamanho, total,
            (int)Math.Ceiling(total / (double)filtro.Tamanho));
    }

    public async Task FavoritarAsync(int ofertaId, int usuarioId, CancellationToken ct)
    {
        await using var transacao = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ativa = await db.Ofertas.AnyAsync(oferta => oferta.Id == ofertaId &&
            oferta.Status == StatusOferta.Ativo && oferta.ExcluidoEm == null, ct);
        if (!ativa)
            throw new NotFoundException("Oferta não encontrada.");

        var existe = await db.Favoritos.AnyAsync(favorito =>
            favorito.UsuarioId == usuarioId && favorito.OfertaId == ofertaId, ct);
        if (!existe)
        {
            db.Favoritos.Add(new Favorito { UsuarioId = usuarioId, OfertaId = ofertaId });
            await db.SaveChangesAsync(ct);
        }
        await transacao.CommitAsync(ct);
    }

    public async Task DesfavoritarAsync(int ofertaId, int usuarioId, CancellationToken ct)
    {
        await db.Favoritos.Where(item => item.UsuarioId == usuarioId && item.OfertaId == ofertaId)
            .ExecuteDeleteAsync(ct);
    }
}
