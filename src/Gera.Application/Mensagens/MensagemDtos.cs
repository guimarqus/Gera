using Gera.Domain.Enums;

namespace Gera.Application.Mensagens;

public sealed record EnviarMensagemRequest(string Conteudo);
public sealed record MensagemDto(long Id, int SolicitacaoId, int RemetenteId, string Remetente, TipoMensagem Tipo, string Conteudo, string? MidiaUrl, long? OrcamentoId, bool Lida, DateTime DataHora);
public sealed record ConversaDto(int SolicitacaoId, string Titulo, int OutroUsuarioId, string OutroUsuario, string? FotoUrl, EtapaKanban Etapa, string? UltimaMensagem, DateTime? UltimaMensagemEm, int NaoLidas);
public sealed record NotificacaoDto(int Id, string Titulo, string Mensagem, TipoNotificacao Tipo, int? ReferenciaId, string? Link, bool Lida, DateTime CriadoEm);
public sealed record NotificacoesPaginaDto(IReadOnlyList<NotificacaoDto> Itens, int NaoLidas, int Pagina, int TotalPaginas);
public sealed record PreferenciasNotificacaoDto(bool Ativas);
