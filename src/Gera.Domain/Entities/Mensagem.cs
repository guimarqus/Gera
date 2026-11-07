using Gera.Domain.Enums;

namespace Gera.Domain.Entities;

public class Mensagem
{
    public long Id { get; set; }

    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;

    public int RemetenteId { get; set; }
    public Usuario Remetente { get; set; } = null!;

    public TipoMensagem Tipo { get; set; } = TipoMensagem.Texto;
    public string Conteudo { get; set; } = string.Empty;
    public string? MidiaUrl { get; set; }

    public int? OrcamentoId { get; set; }
    public Orcamento? Orcamento { get; set; }

    public bool Lida { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
}

