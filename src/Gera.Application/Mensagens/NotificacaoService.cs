using Gera.Application.Interfaces;
using Gera.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Mensagens;

public sealed class NotificacaoService(IAppDbContext db)
{
    public async Task<NotificacoesPaginaDto> ListarAsync(int usuarioId, int pagina, int tamanho, CancellationToken ct)
    {
        if (pagina is < 1 or > 1000000 || tamanho is < 1 or > 50) throw new DomainException("Paginação inválida.");
        var query = db.Notificacoes.AsNoTracking().Where(n => n.UsuarioId == usuarioId);
        var total = await query.CountAsync(ct);
        var naoLidas = await query.CountAsync(n => !n.Lida, ct);
        var itens = await query.OrderByDescending(n => n.CriadoEm).Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(n => new NotificacaoDto(n.Id, n.Titulo, n.Mensagem, n.Tipo, n.ReferenciaId, n.Link, n.Lida, n.CriadoEm)).ToListAsync(ct);
        return new NotificacoesPaginaDto(itens, naoLidas, pagina, Math.Max(1, (int)Math.Ceiling(total / (double)tamanho)));
    }

    public async Task MarcarLidaAsync(int usuarioId, int id, CancellationToken ct)
    {
        var alteradas = await db.Notificacoes.Where(n => n.Id == id && n.UsuarioId == usuarioId)
            .ExecuteUpdateAsync(update => update.SetProperty(n => n.Lida, true), ct);
        if (alteradas == 0 && !await db.Notificacoes.AnyAsync(n => n.Id == id && n.UsuarioId == usuarioId, ct))
            throw new NotFoundException("Notificação", id);
    }

    public async Task MarcarTodasLidasAsync(int usuarioId, CancellationToken ct) =>
        _ = await db.Notificacoes.Where(n => n.UsuarioId == usuarioId && !n.Lida)
            .ExecuteUpdateAsync(update => update.SetProperty(n => n.Lida, true), ct);

    public async Task<PreferenciasNotificacaoDto> PreferenciasAsync(int usuarioId, CancellationToken ct)
    {
        var ativa = await db.Usuarios.Where(u => u.Id == usuarioId).Select(u => (bool?)u.NotificacoesAtivas).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Usuário", usuarioId);
        return new PreferenciasNotificacaoDto(ativa);
    }

    public async Task<PreferenciasNotificacaoDto> AlterarPreferenciasAsync(int usuarioId, bool ativas, CancellationToken ct)
    {
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId, ct) ?? throw new NotFoundException("Usuário", usuarioId);
        usuario.NotificacoesAtivas = ativas;
        usuario.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new PreferenciasNotificacaoDto(ativas);
    }
}
