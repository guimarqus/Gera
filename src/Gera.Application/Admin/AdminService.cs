using System.Data;
using Gera.Application.Catalog;
using Gera.Application.Interfaces;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gera.Application.Admin;

public sealed class AdminService(IAppDbContext db, UserManager<Usuario> users, ICategoriasService categorias)
{
    public async Task<AdminResumoDto> ResumoAsync(CancellationToken ct) => new(
        await db.Usuarios.CountAsync(u => u.Ativo, ct),
        await db.Ofertas.CountAsync(o => o.Status == StatusOferta.Ativo && o.ExcluidoEm == null, ct),
        await db.Denuncias.CountAsync(d => d.Status == StatusDenuncia.Pendente, ct),
        await db.Solicitacoes.CountAsync(s => s.Etapa != EtapaKanban.Concluidos && s.Etapa != EtapaKanban.Cancelado, ct));

    public async Task<IReadOnlyList<DenunciaAdminDto>> DenunciasAsync(StatusDenuncia? status, int pagina, int tamanho, CancellationToken ct)
    {
        ValidarPaginacao(pagina, tamanho);
        if (status.HasValue && !Enum.IsDefined(status.Value)) throw new DomainException("Status de denúncia inválido.");
        var query = db.Denuncias.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(d => d.Status == status);
        var rows = await query.OrderBy(d => d.Status).ThenBy(d => d.CriadoEm).Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(d => new DenunciaLinha(d.Id, d.DenuncianteId, d.Denunciante.Nome, d.TipoAlvo, d.AlvoId,
                d.Motivo, d.Descricao, d.Status, d.ParecerModerador, d.ModeradorId, d.CriadoEm, d.ResolvidoEm)).ToListAsync(ct);
        var ids = rows.Select(r => r.AlvoId).Distinct().ToArray();
        var ofertas = await db.Ofertas.AsNoTracking().Where(o => ids.Contains(o.Id)).Select(o => new { o.Id, o.Titulo }).ToDictionaryAsync(o => o.Id, o => o.Titulo, ct);
        var ofertasEmModeracao = await db.Ofertas.AsNoTracking().Where(o => ids.Contains(o.Id) && o.Status == StatusOferta.Moderacao).Select(o => o.Id).ToHashSetAsync(ct);
        var usuarios = await db.Usuarios.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => new { u.Id, u.Nome }).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
        var mensagens = await db.Mensagens.AsNoTracking().Where(m => ids.Contains((int)m.Id)).Select(m => new { Id = (int)m.Id, m.Conteudo }).ToDictionaryAsync(m => m.Id, m => m.Conteudo, ct);
        return rows.Select(r => new DenunciaAdminDto(r.Id, r.DenuncianteId, r.Denunciante, r.TipoAlvo, r.AlvoId,
            r.TipoAlvo switch
            {
                TipoAlvoDenuncia.Oferta when ofertas.TryGetValue(r.AlvoId, out var titulo) => titulo,
                TipoAlvoDenuncia.Usuario when usuarios.TryGetValue(r.AlvoId, out var nome) => nome,
                TipoAlvoDenuncia.Mensagem when mensagens.TryGetValue(r.AlvoId, out var conteudo) => conteudo.Length > 250 ? conteudo[..250] : conteudo,
                _ => "Conteúdo indisponível"
            }, r.TipoAlvo == TipoAlvoDenuncia.Oferta && ofertasEmModeracao.Contains(r.AlvoId), r.Motivo, r.Descricao, r.Status, r.ParecerModerador, r.ModeradorId, r.CriadoEm, r.ResolvidoEm)).ToArray();
    }

    public async Task DecidirDenunciaAsync(int adminId, int denunciaId, DenunciaDecisaoRequest request, CancellationToken ct)
    {
        if (request.Status is not (StatusDenuncia.EmAnalise or StatusDenuncia.Aceita or StatusDenuncia.Rejeitada))
            throw new DomainException("A decisão deve encaminhar para análise, aceitar ou rejeitar.");
        var parecer = request.Parecer?.Trim() ?? "";
        if (parecer.Length is < 3 or > 1000) throw new DomainException("O parecer deve ter entre 3 e 1.000 caracteres.");
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var denuncia = await db.Denuncias.SingleOrDefaultAsync(d => d.Id == denunciaId, ct) ?? throw new NotFoundException("Denúncia", denunciaId);
        if (denuncia.Status is StatusDenuncia.Aceita or StatusDenuncia.Rejeitada)
            throw new DomainException("Uma denúncia encerrada não pode ser alterada.");
        denuncia.Status = request.Status;
        denuncia.ParecerModerador = parecer;
        denuncia.ModeradorId = adminId;
        denuncia.ResolvidoEm = request.Status is StatusDenuncia.Aceita or StatusDenuncia.Rejeitada ? DateTime.UtcNow : null;
        Registrar(adminId, "DecidirDenuncia", "Denuncia", denunciaId, parecer);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<CategoriaAdminDto>> CategoriasAsync(CancellationToken ct) =>
        await db.Categorias.AsNoTracking().OrderBy(c => c.CategoriaPaiId).ThenBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new CategoriaAdminDto(c.Id, c.CategoriaPaiId, c.Nome, c.Descricao, c.Icone, c.Ordem, c.Ativo)).ToListAsync(ct);

    public async Task<CategoriaAdminDto> CriarCategoriaAsync(int adminId, CategoriaAdminRequest request, CancellationToken ct)
    {
        ValidarCategoria(request);
        if (request.CategoriaPaiId is int parent)
        {
            var parentActive = await db.Categorias.Where(c => c.Id == parent).Select(c => (bool?)c.Ativo).SingleOrDefaultAsync(ct)
                ?? throw new NotFoundException("Categoria pai", parent);
            if (request.Ativo && !parentActive) throw new DomainException("Uma subcategoria ativa precisa de uma categoria pai ativa.");
        }
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var categoria = new Categoria { CategoriaPaiId = request.CategoriaPaiId, Nome = request.Nome.Trim(), Descricao = TrimOrNull(request.Descricao), Icone = TrimOrNull(request.Icone), Ordem = request.Ordem, Ativo = request.Ativo };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);
        Registrar(adminId, "CriarCategoria", "Categoria", categoria.Id, categoria.Nome);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        categorias.InvalidarCache();
        return Map(categoria);
    }

    public async Task<CategoriaAdminDto> EditarCategoriaAsync(int adminId, int id, CategoriaAdminRequest request, CancellationToken ct)
    {
        ValidarCategoria(request);
        var categoria = await db.Categorias.SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Categoria", id);
        if (request.CategoriaPaiId == id) throw new DomainException("Uma categoria não pode ser pai de si mesma.");
        if (request.CategoriaPaiId is int parent)
        {
            if (request.Ativo && !await db.Categorias.AnyAsync(c => c.Id == parent && c.Ativo, ct))
                throw new DomainException("Uma subcategoria ativa precisa de uma categoria pai ativa.");
            var cursor = await db.Categorias.Where(c => c.Id == parent).Select(c => c.CategoriaPaiId).SingleOrDefaultAsync(ct);
            if (!await db.Categorias.AnyAsync(c => c.Id == parent, ct)) throw new NotFoundException("Categoria pai", parent);
            while (cursor is int ancestor)
            {
                if (ancestor == id) throw new DomainException("A alteração criaria um ciclo na árvore de categorias.");
                cursor = await db.Categorias.Where(c => c.Id == ancestor).Select(c => c.CategoriaPaiId).SingleOrDefaultAsync(ct);
            }
        }
        if (!request.Ativo && await db.Categorias.AnyAsync(c => c.CategoriaPaiId == id && c.Ativo, ct))
            throw new DomainException("Desative as subcategorias ativas antes de desativar esta categoria.");
        categoria.CategoriaPaiId = request.CategoriaPaiId;
        categoria.Nome = request.Nome.Trim();
        categoria.Descricao = TrimOrNull(request.Descricao);
        categoria.Icone = TrimOrNull(request.Icone);
        categoria.Ordem = request.Ordem;
        categoria.Ativo = request.Ativo;
        Registrar(adminId, "EditarCategoria", "Categoria", id, categoria.Nome);
        await db.SaveChangesAsync(ct);
        categorias.InvalidarCache();
        return Map(categoria);
    }

    public async Task<IReadOnlyList<UsuarioAdminDto>> UsuariosAsync(string? busca, int pagina, int tamanho, CancellationToken ct)
    {
        ValidarPaginacao(pagina, tamanho);
        var query = db.Usuarios.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            if (termo.Length > 100) throw new DomainException("A busca deve ter até 100 caracteres.");
            query = query.Where(u => u.Nome.Contains(termo) || (u.Email != null && u.Email.Contains(termo)));
        }
        return await query.OrderByDescending(u => u.CriadoEm).Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(u => new UsuarioAdminDto(u.Id, u.Nome, u.Email, u.Ativo, u.Verificado, u.CriadoEm)).ToListAsync(ct);
    }

    public async Task AlterarStatusUsuarioAsync(int adminId, int usuarioId, StatusUsuarioRequest request, CancellationToken ct)
    {
        var motivo = request.Motivo?.Trim() ?? "";
        if (motivo.Length is < 3 or > 1000) throw new DomainException("O motivo deve ter entre 3 e 1.000 caracteres.");
        if (adminId == usuarioId) throw new DomainException("Você não pode alterar o próprio status.");
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var usuario = await users.FindByIdAsync(usuarioId.ToString()) ?? throw new NotFoundException("Usuário", usuarioId);
        if (await users.IsInRoleAsync(usuario, "Administrador")) throw new DomainException("O status de uma conta administradora não pode ser alterado por este painel.");
        if (usuario.Ativo == request.Ativo) { await transaction.CommitAsync(ct); return; }
        usuario.Ativo = request.Ativo;
        usuario.AtualizadoEm = DateTime.UtcNow;
        if (!request.Ativo)
            await db.RefreshTokens.Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null)
                .ExecuteUpdateAsync(update => update.SetProperty(t => t.RevogadoEm, DateTime.UtcNow), ct);
        Registrar(adminId, request.Ativo ? "AtivarUsuario" : "SuspenderUsuario", "Usuario", usuarioId, motivo);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task AlterarVerificacaoAsync(int adminId, int usuarioId, VerificarUsuarioRequest request, CancellationToken ct)
    {
        var motivo = request.Motivo?.Trim() ?? "";
        if (motivo.Length is < 3 or > 1000) throw new DomainException("Registre uma justificativa de validação entre 3 e 1.000 caracteres.");
        if (adminId == usuarioId) throw new DomainException("Você não pode conceder ou remover o próprio selo.");
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var usuario = await users.FindByIdAsync(usuarioId.ToString()) ?? throw new NotFoundException("Usuário", usuarioId);
        if (usuario.Verificado == request.Verificado) { await transaction.CommitAsync(ct); return; }
        usuario.Verificado = request.Verificado;
        usuario.AtualizadoEm = DateTime.UtcNow;
        Registrar(adminId, request.Verificado ? "VerificarUsuario" : "RemoverVerificacaoUsuario", "Usuario", usuarioId, motivo);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ModerarOfertaAsync(int adminId, int ofertaId, ModerarOfertaRequest request, CancellationToken ct)
    {
        var motivo = request.Motivo?.Trim() ?? "";
        if (motivo.Length is < 3 or > 1000) throw new DomainException("O motivo deve ter entre 3 e 1.000 caracteres.");
        var oferta = await db.Ofertas.SingleOrDefaultAsync(o => o.Id == ofertaId && o.ExcluidoEm == null, ct)
            ?? throw new NotFoundException("Oferta", ofertaId);
        if (request.Ativa && oferta.PublicadoEm is null) throw new DomainException("Uma oferta que nunca foi publicada não pode ser ativada pela moderação.");
        oferta.Status = request.Ativa ? StatusOferta.Ativo : StatusOferta.Moderacao;
        oferta.AtualizadoEm = DateTime.UtcNow;
        Registrar(adminId, request.Ativa ? "ReativarOferta" : "RetirarOferta", "Oferta", ofertaId, motivo);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditoriaAdminDto>> AuditoriasAsync(int pagina, int tamanho, CancellationToken ct)
    {
        ValidarPaginacao(pagina, tamanho);
        return await db.AuditoriasAdmin.AsNoTracking().OrderByDescending(a => a.CriadoEm).ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(a => new AuditoriaAdminDto(a.Id, a.AdministradorId, a.Acao, a.Entidade, a.Identificador, a.Motivo, a.CriadoEm)).ToListAsync(ct);
    }

    private void Registrar(int adminId, string acao, string entidade, object id, string? motivo) =>
        db.AuditoriasAdmin.Add(new AuditoriaAdmin { AdministradorId = adminId, Acao = acao, Entidade = entidade, Identificador = id.ToString()!, Motivo = motivo });

    private static CategoriaAdminDto Map(Categoria c) => new(c.Id, c.CategoriaPaiId, c.Nome, c.Descricao, c.Icone, c.Ordem, c.Ativo);
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ValidarPaginacao(int pagina, int tamanho)
    {
        if (pagina is < 1 or > 1_000_000 || tamanho is < 1 or > 50) throw new DomainException("Paginação inválida.");
    }
    private static void ValidarCategoria(CategoriaAdminRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome) || r.Nome.Trim().Length > 100 || r.Descricao?.Length > 300 || r.Icone?.Length > 50 || r.Ordem < 0)
            throw new DomainException("Confira nome, descrição, ícone e ordem da categoria.");
    }

    private sealed record DenunciaLinha(int Id, int DenuncianteId, string Denunciante, TipoAlvoDenuncia TipoAlvo, int AlvoId, string Motivo, string? Descricao, StatusDenuncia Status, string? ParecerModerador, int? ModeradorId, DateTime CriadoEm, DateTime? ResolvidoEm);
}
