namespace Gera.Domain.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Icone { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;

    public int? CategoriaPaiId { get; set; }
    public Categoria? CategoriaPai { get; set; }

    public ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();
    public ICollection<Oferta> Ofertas { get; set; } = new List<Oferta>();
}

