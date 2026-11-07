using Gera.Domain.Enums;

namespace Gera.Domain.Entities;

public class SolicitacaoHistorico
{
    public int Id { get; set; }

    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public EtapaKanban EtapaAnterior { get; set; }
    public EtapaKanban EtapaNova { get; set; }

    public string? Observacao { get; set; }
    public DateTime OcorreuEm { get; set; } = DateTime.UtcNow;
}

