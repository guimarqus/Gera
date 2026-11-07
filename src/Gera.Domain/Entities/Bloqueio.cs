namespace Gera.Domain.Entities;

public class Bloqueio
{
    public int Id { get; set; }

    public int BloqueadorId { get; set; }
    public Usuario Bloqueador { get; set; } = null!;

    public int BloqueadoId { get; set; }
    public Usuario Bloqueado { get; set; } = null!;

    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

