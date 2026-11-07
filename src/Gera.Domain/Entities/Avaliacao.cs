namespace Gera.Domain.Entities;

public class Avaliacao
{
    public int Id { get; set; }

    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;

    public int AvaliadorId { get; set; }
    public Usuario Avaliador { get; set; } = null!;

    public int AvaliadoId { get; set; }
    public Usuario Avaliado { get; set; } = null!;

    /// <summary>
    /// Nota de 1 a 5 estrelas. Restrita por CHECK constraint no banco.
    /// </summary>
    public int Nota { get; set; }

    public string? Comentario { get; set; }

    /// <summary>
    /// Resposta do avaliado à avaliação recebida (uma resposta por avaliação).
    /// </summary>
    public string? Resposta { get; set; }
    public DateTime? RespondidoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

