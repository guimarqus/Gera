namespace Gera.Domain.Enums;

/// <summary>
/// Etapas do fluxo do Kanban de Serviços.
/// Transições passam exclusivamente por SolicitacaoService.AvancarEtapa.
/// </summary>
public enum EtapaKanban
{
    Solicitacoes = 1,
    EmNegociacao = 2,
    Agendados = 3,
    EmExecucao = 4,
    Concluidos = 5,
    Cancelado = 6
}

