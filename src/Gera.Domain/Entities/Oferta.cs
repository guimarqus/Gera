using Gera.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Gera.Domain.Entities;

public class Oferta
{
    public int Id { get; set; }
    public int PrestadorId { get; set; }
    public Usuario Prestador { get; set; } = null!;

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Coluna espacial geography para consultas com índice espacial.
    /// SRID 4326 (WGS 84).
    /// </summary>
    public Point Localizacao { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RaioKm { get; set; } = 15;
    public string? AreaAtendida { get; set; }

    public TipoPreco TipoPreco { get; set; } = TipoPreco.Diaria;
    public decimal Valor { get; set; }
    public bool ValorNegociavel { get; set; }
    public string? Disponibilidade { get; set; }
    public string? FormasPagamento { get; set; }
    public StatusOferta Status { get; set; } = StatusOferta.Ativo;

    public double NotaMedia { get; set; } = 5.0;
    public int TotalAvaliacoes { get; set; }
    public int TotalServicos { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<OfertaFoto> Fotos { get; set; } = new List<OfertaFoto>();
    public ICollection<OfertaTag> Tags { get; set; } = new List<OfertaTag>();
    public ICollection<Solicitacao> Solicitacoes { get; set; } = new List<Solicitacao>();
    public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
}

