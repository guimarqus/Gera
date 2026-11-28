using Gera.Domain.Enums;

namespace Gera.Application.Solicitacoes;

public sealed record CriarSolicitacaoRequest(string Descricao);
public sealed record ProporOrcamentoRequest(decimal Valor, int? PrazoEmDias, DateTime? DataPrevisao, string OQueEstaIncluso, string? Observacoes, string Versao);
public sealed record ResponderOrcamentoRequest(string Versao);
public sealed record MoverSolicitacaoRequest(EtapaKanban Etapa, string Versao, string? Motivo = null);
public sealed record AcaoSolicitacaoRequest(string Versao, string? Motivo = null);
public sealed record OrcamentoDto(int Id, int AutorId, decimal Valor, int? PrazoEmDias, DateTime? DataPrevisao, string OQueEstaIncluso, string? Observacoes, StatusOrcamento Status, DateTime CriadoEm, DateTime? RespondidoEm);
public sealed record HistoricoSolicitacaoDto(int UsuarioId, EtapaKanban EtapaAnterior, EtapaKanban EtapaNova, string? Observacao, DateTime OcorreuEm);
public sealed record SolicitacaoDto(int Id, int ClienteId, string ClienteNome, int PrestadorId, string PrestadorNome, int OfertaId, string OfertaTitulo, string Descricao, EtapaKanban Etapa, decimal? Valor, DateTime? DataAgendada, bool ConcluidoPorPrestador, bool ConcluidoPorCliente, string Versao, DateTime CriadoEm, string? MotivoCancelamento, IReadOnlyList<OrcamentoDto> Orcamentos, IReadOnlyList<HistoricoSolicitacaoDto> Historicos);
