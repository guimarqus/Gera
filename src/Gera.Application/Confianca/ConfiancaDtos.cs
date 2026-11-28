using Gera.Domain.Enums;

namespace Gera.Application.Confianca;

public sealed record AvaliarRequest(int Nota, string? Comentario);
public sealed record ResponderAvaliacaoRequest(string Resposta);
public sealed record DenunciarRequest(TipoAlvoDenuncia TipoAlvo, int AlvoId, string Motivo, string? Descricao);
public sealed record BloquearRequest(string? Motivo);
public sealed record AvaliacaoPublicaDto(int Id, string Avaliador, int Nota, string? Comentario, string? Resposta, DateTime CriadoEm);
public sealed record AvaliacaoDto(int Id, int SolicitacaoId, int AvaliadorId, int AvaliadoId, int Nota, string? Comentario, string? Resposta, DateTime CriadoEm, DateTime? RespondidoEm);
public sealed record BloqueioDto(int UsuarioId, string Nome, string? FotoUrl, DateTime CriadoEm, string? Motivo);
