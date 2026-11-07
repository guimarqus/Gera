namespace ServicosGerais.Domain.Enums;

public enum EtapaKanban : byte
{
    Solicitacoes = 0,
    EmNegociacao = 1,
    Agendados = 2,
    EmExecucao = 3,
    Concluidos = 4,
    Cancelados = 5
}

