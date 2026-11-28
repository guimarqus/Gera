using Gera.Application.Interfaces;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Mensagens;

public sealed class MensagemService(IAppDbContext db, IArmazenamentoChat midia)
{
    public async Task<IReadOnlyList<ConversaDto>> ConversasAsync(int usuarioId, CancellationToken ct) =>
        await db.Solicitacoes.AsNoTracking().Where(s => (s.ClienteId == usuarioId || s.PrestadorId == usuarioId) &&
                !db.Bloqueios.Any(b => (b.BloqueadorId == s.ClienteId && b.BloqueadoId == s.PrestadorId) || (b.BloqueadorId == s.PrestadorId && b.BloqueadoId == s.ClienteId)))
            .OrderByDescending(s => s.Mensagens.OrderByDescending(m => m.Id).Select(m => (DateTime?)m.DataHora).FirstOrDefault()).Take(100)
            .Select(s => new ConversaDto(s.Id, s.Oferta.Titulo,
                usuarioId == s.ClienteId ? s.PrestadorId : s.ClienteId,
                usuarioId == s.ClienteId ? s.Prestador.Nome : s.Cliente.Nome,
                usuarioId == s.ClienteId ? s.Prestador.FotoUrl : s.Cliente.FotoUrl,
                s.Etapa,
                s.Mensagens.OrderByDescending(m => m.Id).Select(m => m.Tipo == TipoMensagem.Foto ? "Foto" : m.Tipo == TipoMensagem.Audio ? "Áudio" : m.Conteudo).FirstOrDefault(),
                s.Mensagens.OrderByDescending(m => m.Id).Select(m => (DateTime?)m.DataHora).FirstOrDefault(),
                s.Mensagens.Count(m => m.RemetenteId != usuarioId && !m.Lida))).ToListAsync(ct);

    public async Task<IReadOnlyList<MensagemDto>> HistoricoAsync(int usuarioId, int solicitacaoId, long? antesDe, CancellationToken ct)
    {
        await ExigirParticipanteAsync(usuarioId, solicitacaoId, ct);
        var query = db.Mensagens.AsNoTracking().Where(m => m.SolicitacaoId == solicitacaoId);
        if (antesDe is > 0) query = query.Where(m => m.Id < antesDe.Value);
        var mensagens = await query.OrderByDescending(m => m.Id).Take(50)
            .Select(m => new MensagemDto(m.Id, m.SolicitacaoId, m.RemetenteId, m.Remetente.Nome, m.Tipo, m.Conteudo, m.MidiaUrl, m.OrcamentoId, m.Lida, m.DataHora)).ToListAsync(ct);
        return mensagens.OrderBy(m => m.Id).ToArray();
    }

    public Task<MensagemDto> EnviarTextoAsync(int usuarioId, int solicitacaoId, string conteudo, CancellationToken ct)
    {
        var texto = conteudo?.Trim() ?? "";
        if (texto.Length is < 1 or > 2000) throw new DomainException("A mensagem deve ter entre 1 e 2.000 caracteres.");
        return CriarAsync(usuarioId, solicitacaoId, TipoMensagem.Texto, texto, null, ct);
    }

    public async Task<MensagemDto> EnviarMidiaAsync(int usuarioId, int solicitacaoId, Stream arquivo, long tamanho, CancellationToken ct)
    {
        await ExigirParticipanteAsync(usuarioId, solicitacaoId, ct);
        var salvo = await midia.SalvarAsync(arquivo, tamanho, ct);
        var tipo = salvo.ContentType.StartsWith("image/", StringComparison.Ordinal) ? TipoMensagem.Foto : TipoMensagem.Audio;
        try { return await CriarAsync(usuarioId, solicitacaoId, tipo, tipo == TipoMensagem.Foto ? "Foto" : "Áudio", salvo.Url, ct); }
        catch { midia.Remover(salvo.Url); throw; }
    }

    public async Task<bool> PodeAcessarAsync(int usuarioId, int solicitacaoId, CancellationToken ct) =>
        await db.Solicitacoes.AnyAsync(s => s.Id == solicitacaoId && (s.ClienteId == usuarioId || s.PrestadorId == usuarioId) &&
            !db.Bloqueios.Any(b => (b.BloqueadorId == s.ClienteId && b.BloqueadoId == s.PrestadorId) || (b.BloqueadorId == s.PrestadorId && b.BloqueadoId == s.ClienteId)), ct);

    public async Task<int> OutroParticipanteAsync(int usuarioId, int solicitacaoId, CancellationToken ct)
    {
        var id = await db.Solicitacoes.Where(s => s.Id == solicitacaoId && (s.ClienteId == usuarioId || s.PrestadorId == usuarioId))
            .Select(s => s.ClienteId == usuarioId ? s.PrestadorId : s.ClienteId).SingleOrDefaultAsync(ct);
        return id != 0 ? id : throw new NotFoundException("Solicitação", solicitacaoId);
    }

    public async Task<int> MarcarLidasAsync(int usuarioId, int solicitacaoId, CancellationToken ct)
    {
        await ExigirParticipanteAsync(usuarioId, solicitacaoId, ct);
        return await db.Mensagens.Where(m => m.SolicitacaoId == solicitacaoId && m.RemetenteId != usuarioId && !m.Lida)
            .ExecuteUpdateAsync(update => update.SetProperty(m => m.Lida, true), ct);
    }

    public async Task<(Stream Arquivo, string ContentType)> AbrirMidiaAsync(int usuarioId, Guid id, CancellationToken ct)
    {
        var url = await db.Mensagens.AsNoTracking().Where(m => m.MidiaUrl == $"/api/mensagens/midia/{id:N}" &&
                (m.Solicitacao.ClienteId == usuarioId || m.Solicitacao.PrestadorId == usuarioId) &&
                !db.Bloqueios.Any(b => (b.BloqueadorId == m.Solicitacao.ClienteId && b.BloqueadoId == m.Solicitacao.PrestadorId) ||
                    (b.BloqueadorId == m.Solicitacao.PrestadorId && b.BloqueadoId == m.Solicitacao.ClienteId)))
            .Select(m => m.MidiaUrl).SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Mídia", id);
        return midia.Abrir(id) ?? throw new NotFoundException("Mídia", url);
    }

    private async Task<MensagemDto> CriarAsync(int usuarioId, int solicitacaoId, TipoMensagem tipo, string conteudo, string? midiaUrl, CancellationToken ct)
    {
        var solicitacao = await db.Solicitacoes.Include(s => s.Cliente).Include(s => s.Prestador)
            .SingleOrDefaultAsync(s => s.Id == solicitacaoId && (s.ClienteId == usuarioId || s.PrestadorId == usuarioId), ct)
            ?? throw new NotFoundException("Solicitação", solicitacaoId);
        if (await db.Bloqueios.AnyAsync(b => (b.BloqueadorId == solicitacao.ClienteId && b.BloqueadoId == solicitacao.PrestadorId) ||
            (b.BloqueadorId == solicitacao.PrestadorId && b.BloqueadoId == solicitacao.ClienteId), ct))
            throw new NotFoundException("Solicitação", solicitacaoId);
        var remetente = usuarioId == solicitacao.ClienteId ? solicitacao.Cliente : solicitacao.Prestador;
        var destinatario = usuarioId == solicitacao.ClienteId ? solicitacao.Prestador : solicitacao.Cliente;
        var message = new Mensagem { SolicitacaoId = solicitacaoId, RemetenteId = usuarioId, Tipo = tipo, Conteudo = conteudo, MidiaUrl = midiaUrl };
        db.Mensagens.Add(message);
        if (destinatario.NotificacoesAtivas)
            db.Notificacoes.Add(new Notificacao { UsuarioId = destinatario.Id, Titulo = "Nova mensagem", Mensagem = $"{remetente.Nome} enviou uma mensagem.", Tipo = TipoNotificacao.NovaMensagem, ReferenciaId = solicitacaoId, Link = "/mensagens" });
        await db.SaveChangesAsync(ct);
        return new MensagemDto(message.Id, solicitacaoId, usuarioId, remetente.Nome, tipo, conteudo, midiaUrl, null, false, message.DataHora);
    }

    private async Task ExigirParticipanteAsync(int usuarioId, int solicitacaoId, CancellationToken ct)
    {
        if (!await PodeAcessarAsync(usuarioId, solicitacaoId, ct)) throw new NotFoundException("Solicitação", solicitacaoId);
    }
}
