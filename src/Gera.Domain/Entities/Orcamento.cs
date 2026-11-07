using Gera.Domain.Enums;

namespace Gera.Domain.Entities;

public class Orcamento
{
    public int Id { get; set; }

    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;

    public int PrestadorId { get; set; }
    public Usuario Prestador { get; set; } = null!;

    public decimal Valor { get; set; }
    public int? PrazoEmDias { get; set; }
    public DateTime? DataPrevisao { get; set; }
    public string OQueEstaIncluso { get; set; } = string.Empty;
    public string? Observacoes { get; set; }

    public StatusOrcamento Status { get; set; } = StatusOrcamento.Pendente;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? RespondidoEm { get; set; }
}

