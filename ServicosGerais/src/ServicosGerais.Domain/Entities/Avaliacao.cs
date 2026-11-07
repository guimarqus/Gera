namespace ServicosGerais.Domain.Entities;

public class Avaliacao
{
    public int Id { get; set; }
    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;
    public int AvaliadorId { get; set; }
    public Usuario Avaliador { get; set; } = null!;
    public int AvaliadoId { get; set; }
    public Usuario Avaliado { get; set; } = null!;
    public int Nota { get; set; } // 1-5
    public string? Comentario { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

