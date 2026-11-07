using Microsoft.AspNetCore.Identity;

namespace ServicosGerais.Domain.Entities;

public class Usuario : IdentityUser<int>
{
    public string Nome { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Oferta> Ofertas { get; set; } = new List<Oferta>();
    public ICollection<Solicitacao> SolicitacoesComoCliente { get; set; } = new List<Solicitacao>();
    public ICollection<Avaliacao> AvaliacoesFeitas { get; set; } = new List<Avaliacao>();
    public ICollection<Avaliacao> AvaliacoesRecebidas { get; set; } = new List<Avaliacao>();
}

