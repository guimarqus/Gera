using Gera.Domain.Enums;

namespace Gera.Domain.Entities;

public class Solicitacao
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Usuario Cliente { get; set; } = null!;

    public int PrestadorId { get; set; }
    public Usuario Prestador { get; set; } = null!;

    public int OfertaId { get; set; }
    public Oferta Oferta { get; set; } = null!;

    public string Descricao { get; set; } = string.Empty;
    public EtapaKanban Etapa { get; set; } = EtapaKanban.Solicitacoes;

    public decimal? Valor { get; set; }
    public DateTime? DataAgendada { get; set; }

    public bool AceiteCliente { get; set; }
    public bool AceitePrestador { get; set; }
    public bool ConcluidoPorCliente { get; set; }
    public bool ConcluidoPorPrestador { get; set; }

    public string? MotivoCancelamento { get; set; }
    public int? CanceladoPorId { get; set; }

    /// <summary>
    /// Controle de concorrência otimista (dois usuários movendo o mesmo card).
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    public ICollection<SolicitacaoHistorico> Historicos { get; set; } = new List<SolicitacaoHistorico>();
    public ICollection<Orcamento> Orcamentos { get; set; } = new List<Orcamento>();
    public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
}

