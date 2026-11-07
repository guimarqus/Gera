using Gera.Domain.Enums;

namespace Gera.Domain.Entities;

public class Pagamento
{
    public int Id { get; set; }

    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;

    public int PagadorId { get; set; }
    public Usuario Pagador { get; set; } = null!;

    public int RecebedorId { get; set; }
    public Usuario Recebedor { get; set; } = null!;

    public decimal Valor { get; set; }
    public MetodoPagamento Metodo { get; set; } = MetodoPagamento.Pix;
    public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

    public string? IdentificadorTransacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? PagoEm { get; set; }
}

