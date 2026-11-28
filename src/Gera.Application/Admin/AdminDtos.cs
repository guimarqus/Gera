using Gera.Domain.Enums;

namespace Gera.Application.Admin;

public sealed record AdminResumoDto(int UsuariosAtivos, int OfertasAtivas, int DenunciasPendentes, int SolicitacoesAbertas);
public sealed record DenunciaAdminDto(int Id, int DenuncianteId, string Denunciante, TipoAlvoDenuncia TipoAlvo, int AlvoId, string AlvoResumo, bool OfertaEmModeracao, string Motivo, string? Descricao, StatusDenuncia Status, string? ParecerModerador, int? ModeradorId, DateTime CriadoEm, DateTime? ResolvidoEm);
public sealed record CategoriaAdminDto(int Id, int? CategoriaPaiId, string Nome, string? Descricao, string? Icone, int Ordem, bool Ativo);
public sealed record CategoriaAdminRequest(int? CategoriaPaiId, string Nome, string? Descricao, string? Icone, int Ordem, bool Ativo = true);
public sealed record DenunciaDecisaoRequest(StatusDenuncia Status, string Parecer);
public sealed record StatusUsuarioRequest(bool Ativo, string Motivo);
public sealed record ModerarOfertaRequest(bool Ativa, string Motivo);
public sealed record UsuarioAdminDto(int Id, string Nome, string? Email, bool Ativo, bool Verificado, DateTime CriadoEm);
public sealed record AuditoriaAdminDto(long Id, int AdministradorId, string Acao, string Entidade, string Identificador, string? Motivo, DateTime CriadoEm);
