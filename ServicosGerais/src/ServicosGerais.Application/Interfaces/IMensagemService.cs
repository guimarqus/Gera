using ServicosGerais.Application.DTOs.Mensagens;

namespace ServicosGerais.Application.Interfaces;

public interface IMensagemService
{
    Task<List<MensagemDto>> ObterPorSolicitacaoAsync(int solicitacaoId, int usuarioId);
    Task<MensagemDto> EnviarAsync(int solicitacaoId, int remetenteId, EnviarMensagemDto dto);
}

