using Gera.Application.Interfaces;
using Gera.Domain;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Data;

namespace Gera.Application.Ofertas;

public sealed class OfertaService(IAppDbContext db, UserManager<Usuario> usuarios, IOptions<OfertaOptions> options)
{
    private const string PapelPrestador = "Prestador";

    public async Task<OfertaPrivadaDto> CriarAsync(int usuarioId, CancellationToken ct)
    {
        var oferta = new Oferta { PrestadorId = usuarioId };
        db.Ofertas.Add(oferta);
        await db.SaveChangesAsync(ct);
        return Privada(oferta);
    }

    public async Task<IReadOnlyList<OfertaPrivadaDto>> ListarAsync(
        int usuarioId, int pagina, int tamanho, StatusOferta? status, CancellationToken ct)
    {
        var query = Existentes().Where(o => o.PrestadorId == usuarioId);
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var ofertas = await query.AsNoTracking().OrderByDescending(o => o.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync(ct);
        return ofertas.Select(Privada).ToArray();
    }

    public async Task<object> ObterAsync(int id, int? usuarioId, CancellationToken ct)
    {
        var oferta = await Existentes().AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == id && (o.PrestadorId == usuarioId || o.Status == StatusOferta.Ativo), ct)
            ?? throw new NotFoundException("Oferta não encontrada.");
        if (oferta.PrestadorId == usuarioId)
            return Privada(oferta);

        return new OfertaPublicaDto(oferta.Id, oferta.CategoriaId, oferta.Titulo, oferta.Descricao,
            oferta.TipoPreco, oferta.Valor, oferta.Cidade, oferta.Bairro, oferta.Disponibilidade);
    }

    public async Task<OfertaPrivadaDto> EditarAsync(int id, int usuarioId, EditarOfertaRequest dados, CancellationToken ct)
    {
        var oferta = await ProprietariaAsync(id, usuarioId, ct);
        if (oferta.Status == StatusOferta.Moderacao)
            throw new DomainException("Uma oferta em moderação não pode ser editada.");
        if (Convert.ToBase64String(oferta.Versao) != dados.Versao)
            throw new DbUpdateConcurrencyException("A oferta foi alterada em outra sessão.");
        if (dados.CategoriaId is int categoriaId && !await db.Categorias.AnyAsync(c => c.Id == categoriaId && c.Ativo, ct))
            throw new DomainException("Selecione uma categoria ativa.");
        if (dados.TipoPreco is TipoPreco.ACombinar && dados.Valor.HasValue)
            throw new DomainException("A modalidade a combinar não aceita valor.");

        oferta.CategoriaId = dados.CategoriaId;
        oferta.Titulo = dados.Titulo?.Trim() ?? string.Empty;
        oferta.Descricao = dados.Descricao?.Trim() ?? string.Empty;
        oferta.TipoPreco = dados.TipoPreco;
        oferta.Valor = dados.Valor;
        oferta.Latitude = dados.Latitude;
        oferta.Longitude = dados.Longitude;
        oferta.Localizacao = dados.Latitude is double latitude && dados.Longitude is double longitude
            ? GeoFactory.CriarPonto(latitude, longitude)
            : null;
        oferta.RaioKm = dados.RaioKm ?? 15;
        oferta.Cidade = dados.Cidade?.Trim();
        oferta.Bairro = dados.Bairro?.Trim();
        oferta.Disponibilidade = dados.Disponibilidade?.Trim();
        oferta.FormasPagamento = dados.FormasPagamento?.Trim();
        if (oferta.Status == StatusOferta.Ativo)
            await ValidarPublicacaoAsync(oferta, ct);
        oferta.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Privada(oferta);
    }

    public Task<OfertaPrivadaDto> PublicarAsync(int id, int usuarioId, CancellationToken ct) =>
        AtivarAsync(id, usuarioId, StatusOferta.Rascunho, ct);

    public async Task<OfertaPrivadaDto> AlterarStatusAsync(int id, int usuarioId, StatusOferta status, CancellationToken ct)
    {
        if (status == StatusOferta.Ativo)
            return await AtivarAsync(id, usuarioId, StatusOferta.Pausado, ct);
        if (status != StatusOferta.Pausado)
            throw new DomainException("Selecione Ativo ou Pausado.");

        var oferta = await ProprietariaAsync(id, usuarioId, ct);
        if (oferta.Status == StatusOferta.Pausado)
            return Privada(oferta);
        if (oferta.Status != StatusOferta.Ativo)
            throw new DomainException("Somente ofertas ativas podem ser pausadas.");

        oferta.Status = StatusOferta.Pausado;
        oferta.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Privada(oferta);
    }

    private async Task<OfertaPrivadaDto> AtivarAsync(
        int id, int usuarioId, StatusOferta estadoEsperado, CancellationToken ct)
    {
        await using var transacao = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var oferta = await ProprietariaAsync(id, usuarioId, ct);
        if (oferta.Status == StatusOferta.Ativo)
            return Privada(oferta);
        if (oferta.Status != estadoEsperado)
            throw new DomainException(estadoEsperado == StatusOferta.Rascunho
                ? "Somente rascunhos podem ser publicados."
                : "Somente ofertas pausadas podem ser reativadas.");

        await ValidarPublicacaoAsync(oferta, ct);
        var limite = options.Value.LimiteAtivasPorUsuario;
        if (await Existentes().CountAsync(o => o.PrestadorId == usuarioId && o.Status == StatusOferta.Ativo, ct) >= limite)
            throw new DomainException($"O limite de {limite} ofertas ativas foi atingido.");

        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");
        if (!await usuarios.IsInRoleAsync(usuario, PapelPrestador))
        {
            var resultado = await usuarios.AddToRoleAsync(usuario, PapelPrestador);
            if (!resultado.Succeeded)
                throw new InvalidOperationException("Não foi possível conceder o papel Prestador.");
        }

        var agora = DateTime.UtcNow;
        oferta.Status = StatusOferta.Ativo;
        oferta.PublicadoEm ??= agora;
        oferta.AtualizadoEm = agora;
        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return Privada(oferta);
    }

    private async Task ValidarPublicacaoAsync(Oferta oferta, CancellationToken ct)
    {
        if (oferta.Titulo.Length < 5 || oferta.Descricao.Length < 30)
            throw new DomainException("Informe um título de pelo menos 5 caracteres e uma descrição de pelo menos 30.");
        if (oferta.Localizacao is null || oferta.RaioKm is < 1 or > 100 ||
            string.IsNullOrWhiteSpace(oferta.Disponibilidade))
            throw new DomainException("Informe a localização, o raio de atendimento e a disponibilidade.");
        if (oferta.TipoPreco is null || !Enum.IsDefined(oferta.TipoPreco.Value) ||
            (oferta.TipoPreco == TipoPreco.ACombinar ? oferta.Valor.HasValue : oferta.Valor is null or <= 0))
            throw new DomainException("Informe uma modalidade de cobrança e um valor válidos.");
        if (!await db.Categorias.AnyAsync(c => c.Id == oferta.CategoriaId && c.Ativo, ct))
            throw new DomainException("Selecione uma categoria ativa.");
        if (!await db.OfertaFotos.AnyAsync(f => f.OfertaId == oferta.Id, ct))
            throw new DomainException("Adicione pelo menos uma foto para publicar.");
    }

    public async Task ExcluirAsync(int id, int usuarioId, CancellationToken ct)
    {
        var oferta = await ProprietariaAsync(id, usuarioId, ct);
        oferta.ExcluidoEm = oferta.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<Oferta> Existentes() => db.Ofertas.Where(o => o.ExcluidoEm == null);

    private async Task<Oferta> ProprietariaAsync(int id, int usuarioId, CancellationToken ct) =>
        await Existentes().SingleOrDefaultAsync(o => o.Id == id && o.PrestadorId == usuarioId, ct)
            ?? throw new NotFoundException("Oferta não encontrada.");

    private static OfertaPrivadaDto Privada(Oferta o) => new(o.Id, o.CategoriaId, o.Titulo, o.Descricao,
        o.TipoPreco, o.Valor, o.Latitude, o.Longitude, o.RaioKm, o.Cidade, o.Bairro, o.Disponibilidade,
        o.FormasPagamento, o.Status, o.CriadoEm, o.PublicadoEm, Convert.ToBase64String(o.Versao));
}
