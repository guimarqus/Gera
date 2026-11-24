using Gera.Application.Interfaces;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Ofertas;

public sealed record FotoOfertaDto(int Id, string Url, string? UrlMiniatura, int Ordem, bool EhCapa);
public sealed record FotosOfertaDto(string Versao, IReadOnlyList<FotoOfertaDto> Fotos);
public sealed record OrdenarFotosRequest(int[] FotoIds, string Versao);
public sealed record AlterarFotoRequest(string Versao);

public sealed class OfertaFotosService(IAppDbContext db)
{
    public async Task<FotosOfertaDto> ListarAsync(int id, int usuarioId, CancellationToken ct)
    {
        var oferta = await BuscarAsync(id, usuarioId, ct, leitura: true);
        return Resposta(oferta);
    }

    public async Task<FotosOfertaDto> OrdenarAsync(int id, int usuarioId, OrdenarFotosRequest dados, CancellationToken ct)
    {
        var oferta = await BuscarAsync(id, usuarioId, ct);
        ValidarEdicao(oferta, dados.Versao);
        var fotos = oferta.Fotos.ToDictionary(foto => foto.Id);
        if (dados.FotoIds is null || dados.FotoIds.Length != fotos.Count ||
            dados.FotoIds.Distinct().Count() != fotos.Count ||
            dados.FotoIds.Any(fotoId => !fotos.ContainsKey(fotoId)))
            throw new DomainException("Envie todas as fotos da oferta, uma única vez.");

        for (var i = 0; i < dados.FotoIds.Length; i++)
            fotos[dados.FotoIds[i]].Ordem = i + 1;
        return await SalvarAsync(oferta, ct);
    }

    public async Task<FotosOfertaDto> DefinirCapaAsync(int id, int fotoId, int usuarioId, string versao, CancellationToken ct)
    {
        var oferta = await BuscarAsync(id, usuarioId, ct);
        ValidarEdicao(oferta, versao);
        var capa = BuscarFoto(oferta, fotoId);
        foreach (var foto in oferta.Fotos)
            foto.EhCapa = foto == capa;
        return await SalvarAsync(oferta, ct);
    }

    public async Task<FotosOfertaDto> ExcluirAsync(int id, int fotoId, int usuarioId, string versao, CancellationToken ct)
    {
        var oferta = await BuscarAsync(id, usuarioId, ct);
        ValidarEdicao(oferta, versao);
        var foto = BuscarFoto(oferta, fotoId);
        if (oferta.Status == StatusOferta.Ativo && oferta.Fotos.Count == 1)
            throw new DomainException("Uma oferta ativa precisa manter pelo menos uma foto.");

        db.OfertaFotos.Remove(foto);
        oferta.Fotos.Remove(foto);
        var restantes = oferta.Fotos.OrderBy(item => item.Ordem).ThenBy(item => item.Id).ToArray();
        for (var i = 0; i < restantes.Length; i++)
            restantes[i].Ordem = i + 1;
        if (restantes.Length > 0 && !restantes.Any(item => item.EhCapa))
            restantes[0].EhCapa = true;

        return await SalvarAsync(oferta, ct);
    }

    private async Task<Oferta> BuscarAsync(int id, int usuarioId, CancellationToken ct, bool leitura = false)
    {
        var query = db.Ofertas.Include(oferta => oferta.Fotos).AsQueryable();
        if (leitura)
            query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(
            oferta => oferta.Id == id && oferta.PrestadorId == usuarioId && oferta.ExcluidoEm == null, ct)
            ?? throw new NotFoundException("Oferta não encontrada.");
    }

    private static OfertaFoto BuscarFoto(Oferta oferta, int fotoId) =>
        oferta.Fotos.SingleOrDefault(foto => foto.Id == fotoId)
            ?? throw new NotFoundException("Foto não encontrada.");

    private static void ValidarEdicao(Oferta oferta, string versao)
    {
        if (oferta.Status == StatusOferta.Moderacao)
            throw new DomainException("Uma oferta em moderação não pode ser editada.");
        if (Convert.ToBase64String(oferta.Versao) != versao)
            throw new DbUpdateConcurrencyException("A oferta foi alterada em outra sessão.");
    }

    private async Task<FotosOfertaDto> SalvarAsync(Oferta oferta, CancellationToken ct)
    {
        oferta.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Resposta(oferta);
    }

    private static FotosOfertaDto Resposta(Oferta oferta) => new(
        Convert.ToBase64String(oferta.Versao),
        oferta.Fotos.OrderBy(foto => foto.Ordem).ThenBy(foto => foto.Id)
            .Select(foto => new FotoOfertaDto(foto.Id, foto.Url, foto.UrlMiniatura, foto.Ordem, foto.EhCapa))
            .ToArray());
}
