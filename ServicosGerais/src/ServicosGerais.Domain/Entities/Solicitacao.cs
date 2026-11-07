using ServicosGerais.Domain.Enums;

namespace ServicosGerais.Domain.Entities;

public class Solicitacao
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Usuario Cliente { get; set; } = null!;
    public int OfertaId { get; set; }
    public Oferta Oferta { get; set; } = null!;
    public string Descricao { get; set; } = string.Empty;
    public EtapaKanban Etapa { get; set; } = EtapaKanban.Solicitacoes;
    public decimal? Valor { get; set; }
    public DateTime? DataAgendada { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    public ICollection<SolicitacaoHistorico> Historico { get; set; } = new List<SolicitacaoHistorico>();
    public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
}

