namespace Gera.Domain.Entities;

public class OfertaTag
{
    public int Id { get; set; }
    public int OfertaId { get; set; }
    public Oferta Oferta { get; set; } = null!;

    public string Tag { get; set; } = string.Empty;
}

