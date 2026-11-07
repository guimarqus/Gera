namespace ServicosGerais.Domain.Entities;

public class OfertaFoto
{
    public int Id { get; set; }
    public int OfertaId { get; set; }
    public Oferta Oferta { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

