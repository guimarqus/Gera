using ServicosGerais.Application.DTOs.Solicitacoes;

namespace ServicosGerais.Application.Interfaces;

public interface ISolicitacaoService
{
    Task<SolicitacaoDto> CriarAsync(int clienteId, CriarSolicitacaoDto dto);
    Task<List<SolicitacaoKanbanDto>> ObterKanbanAsync(int usuarioId, string papel);
    Task<SolicitacaoDto> AvancarEtapaAsync(int id, int usuarioId, AvancarEtapaDto dto);
    Task CancelarAsync(int id, int usuarioId, string motivo);
}

