namespace Gera.Domain.Entities;

public class Favorito
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int OfertaId { get; set; }
    public Oferta Oferta { get; set; } = null!;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

