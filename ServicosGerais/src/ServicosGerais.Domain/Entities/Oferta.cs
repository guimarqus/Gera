using ServicosGerais.Domain.Enums;

namespace ServicosGerais.Domain.Entities;

public class Oferta
{
    public int Id { get; set; }
    public int PrestadorId { get; set; }
    public Usuario Prestador { get; set; } = null!;
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int RaioKm { get; set; }
    public TipoPreco TipoPreco { get; set; }
    public decimal Valor { get; set; }
    public bool ValorNegociavel { get; set; }
    public StatusOferta Status { get; set; } = StatusOferta.Ativo;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<OfertaFoto> Fotos { get; set; } = new List<OfertaFoto>();
    public ICollection<OfertaTag> Tags { get; set; } = new List<OfertaTag>();
    public ICollection<Solicitacao> Solicitacoes { get; set; } = new List<Solicitacao>();

    // Computed from Avaliacoes
    public double NotaMedia { get; set; }
    public int TotalServicos { get; set; }
}

