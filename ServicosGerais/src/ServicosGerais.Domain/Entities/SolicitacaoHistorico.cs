using ServicosGerais.Domain.Enums;

namespace ServicosGerais.Domain.Entities;

public class SolicitacaoHistorico
{
    public int Id { get; set; }
    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;
    public int UsuarioId { get; set; }
    public EtapaKanban EtapaAnterior { get; set; }
    public EtapaKanban EtapaNova { get; set; }
    public string? Observacao { get; set; }
    public DateTime OcorreuEm { get; set; } = DateTime.UtcNow;
}

