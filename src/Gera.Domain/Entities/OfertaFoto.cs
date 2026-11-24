namespace Gera.Domain.Entities;

public class OfertaFoto
{
    public int Id { get; set; }
    public int OfertaId { get; set; }
    public Oferta Oferta { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string? UrlMiniatura { get; set; }
    public int Ordem { get; set; }
    public bool EhCapa { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

