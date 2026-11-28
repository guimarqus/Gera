using Gera.Application.Interfaces;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Confianca;

public sealed class ConfiancaService(IAppDbContext db)
{
    public async Task<AvaliacaoDto?> MinhaAvaliacaoAsync(int usuarioId, int solicitacaoId, CancellationToken ct)
    {
        if (!await db.Solicitacoes.AnyAsync(s => s.Id == solicitacaoId && (s.ClienteId == usuarioId || s.PrestadorId == usuarioId), ct))
            throw new NotFoundException("Solicitação", solicitacaoId);
        var item = await db.Avaliacoes.AsNoTracking().SingleOrDefaultAsync(a => a.SolicitacaoId == solicitacaoId && a.AvaliadorId == usuarioId, ct);
        return item is null ? null : Map(item);
    }

    public async Task<AvaliacaoDto> AvaliarAsync(int usuarioId, int solicitacaoId, AvaliarRequest request, CancellationToken ct)
    {
        var solicitacao = await db.Solicitacoes.AsNoTracking().SingleOrDefaultAsync(s => s.Id == solicitacaoId &&
            (s.ClienteId == usuarioId || s.PrestadorId == usuarioId), ct) ?? throw new NotFoundException("Solicitação", solicitacaoId);
        if (solicitacao.Etapa != EtapaKanban.Concluidos || !solicitacao.ConcluidoPorCliente || !solicitacao.ConcluidoPorPrestador)
            throw new DomainException("Só é possível avaliar um serviço concluído e confirmado pelas duas partes.");
        if (request.Nota is < 1 or > 5) throw new DomainException("A nota deve ser de 1 a 5.");
        var comentario = request.Comentario?.Trim();
        if (comentario?.Length > 1000) throw new DomainException("O comentário pode ter até 1.000 caracteres.");
        var avaliadoId = usuarioId == solicitacao.ClienteId ? solicitacao.PrestadorId : solicitacao.ClienteId;
        if (await db.Avaliacoes.AnyAsync(a => a.SolicitacaoId == solicitacaoId && a.AvaliadorId == usuarioId, ct))
            throw new DomainException("Você já avaliou este serviço.");
        var avaliacao = new Avaliacao { SolicitacaoId = solicitacaoId, AvaliadorId = usuarioId, AvaliadoId = avaliadoId, Nota = request.Nota, Comentario = comentario };
        db.Avaliacoes.Add(avaliacao);
        await db.SaveChangesAsync(ct);
        return Map(avaliacao);
    }

    public async Task<AvaliacaoDto> ResponderAsync(int usuarioId, int avaliacaoId, ResponderAvaliacaoRequest request, CancellationToken ct)
    {
        var avaliacao = await db.Avaliacoes.AsNoTracking().SingleOrDefaultAsync(a => a.Id == avaliacaoId && a.AvaliadoId == usuarioId, ct)
            ?? throw new NotFoundException("Avaliação", avaliacaoId);
        var resposta = request.Resposta?.Trim() ?? "";
        if (resposta.Length is < 1 or > 1000) throw new DomainException("A resposta deve ter entre 1 e 1.000 caracteres.");
        var agora = DateTime.UtcNow;
        var atualizados = await db.Avaliacoes.Where(a => a.Id == avaliacaoId && a.AvaliadoId == usuarioId && a.Resposta == null)
            .ExecuteUpdateAsync(update => update.SetProperty(a => a.Resposta, resposta).SetProperty(a => a.RespondidoEm, agora), ct);
        if (atualizados == 0) throw new DomainException("Esta avaliação já recebeu uma resposta.");
        avaliacao.Resposta = resposta;
        avaliacao.RespondidoEm = agora;
        return Map(avaliacao);
    }

    public async Task<IReadOnlyList<AvaliacaoPublicaDto>> AvaliacoesPublicasAsync(int prestadorId, int pagina, int tamanho, CancellationToken ct)
    {
        if (pagina is < 1 or > 1_000_000 || tamanho is < 1 or > 50) throw new DomainException("Paginação inválida.");
        return await db.Avaliacoes.AsNoTracking().Where(a => a.AvaliadoId == prestadorId)
            .OrderByDescending(a => a.CriadoEm).ThenByDescending(a => a.Id).Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(a => new AvaliacaoPublicaDto(a.Id, "Cliente do Gera", a.Nota, a.Comentario, a.Resposta, a.CriadoEm)).ToListAsync(ct);
    }

    public async Task DenunciarAsync(int usuarioId, DenunciarRequest request, CancellationToken ct)
    {
        if (request.AlvoId < 1 || !Enum.IsDefined(request.TipoAlvo)) throw new DomainException("Alvo de denúncia inválido.");
        var motivo = request.Motivo?.Trim() ?? "";
        var descricao = request.Descricao?.Trim();
        if (motivo.Length is < 3 or > 150 || descricao?.Length > 1000) throw new DomainException("Informe um motivo válido e descrição com até 1.000 caracteres.");
        var alvoExiste = request.TipoAlvo switch
        {
            TipoAlvoDenuncia.Usuario => await db.Usuarios.AnyAsync(u => u.Id == request.AlvoId && u.Ativo, ct),
            TipoAlvoDenuncia.Oferta => await db.Ofertas.AnyAsync(o => o.Id == request.AlvoId && o.PrestadorId != usuarioId && o.ExcluidoEm == null, ct),
            TipoAlvoDenuncia.Mensagem => await db.Mensagens.AnyAsync(m => m.Id == request.AlvoId && m.RemetenteId != usuarioId &&
                (m.Solicitacao.ClienteId == usuarioId || m.Solicitacao.PrestadorId == usuarioId), ct),
            _ => false
        };
        if (!alvoExiste || (request.TipoAlvo == TipoAlvoDenuncia.Usuario && request.AlvoId == usuarioId))
            throw new NotFoundException("Alvo da denúncia", request.AlvoId);
        var duplicada = await db.Denuncias.AnyAsync(d => d.DenuncianteId == usuarioId && d.TipoAlvo == request.TipoAlvo && d.AlvoId == request.AlvoId &&
            (d.Status == StatusDenuncia.Pendente || d.Status == StatusDenuncia.EmAnalise), ct);
        if (duplicada) throw new DomainException("Você já enviou uma denúncia pendente para este conteúdo.");
        db.Denuncias.Add(new Denuncia { DenuncianteId = usuarioId, TipoAlvo = request.TipoAlvo, AlvoId = request.AlvoId, Motivo = motivo, Descricao = descricao });
        await db.SaveChangesAsync(ct);
    }

    public async Task BloquearAsync(int usuarioId, int alvoId, string? motivo, CancellationToken ct)
    {
        if (alvoId < 1 || alvoId == usuarioId || !await db.Usuarios.AnyAsync(u => u.Id == alvoId && u.Ativo, ct))
            throw new NotFoundException("Usuário", alvoId);
        var texto = motivo?.Trim();
        if (texto?.Length > 300) throw new DomainException("O motivo pode ter até 300 caracteres.");
        if (!await db.Bloqueios.AnyAsync(b => b.BloqueadorId == usuarioId && b.BloqueadoId == alvoId, ct))
        {
            db.Bloqueios.Add(new Bloqueio { BloqueadorId = usuarioId, BloqueadoId = alvoId, Motivo = texto });
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task DesbloquearAsync(int usuarioId, int alvoId, CancellationToken ct)
    {
        var bloqueio = await db.Bloqueios.SingleOrDefaultAsync(b => b.BloqueadorId == usuarioId && b.BloqueadoId == alvoId, ct);
        if (bloqueio is null) return;
        db.Bloqueios.Remove(bloqueio);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BloqueioDto>> BloqueiosAsync(int usuarioId, CancellationToken ct) =>
        await db.Bloqueios.AsNoTracking().Where(b => b.BloqueadorId == usuarioId).OrderByDescending(b => b.CriadoEm)
            .Select(b => new BloqueioDto(b.BloqueadoId, b.Bloqueado.Nome, b.Bloqueado.FotoUrl, b.CriadoEm, b.Motivo)).ToListAsync(ct);

    private static AvaliacaoDto Map(Avaliacao a) => new(a.Id, a.SolicitacaoId, a.AvaliadorId, a.AvaliadoId, a.Nota, a.Comentario, a.Resposta, a.CriadoEm, a.RespondidoEm);
}
