using Microsoft.AspNetCore.Identity;

namespace Gera.Domain.Entities;

public class Usuario : IdentityUser<int>
{
    public string Nome { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string? Biografia { get; set; }
    public string? Documento { get; set; }
    public bool Verificado { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int RaioAtendimentoPadraoKm { get; set; } = 15;
    public bool NotificacoesAtivas { get; set; } = true;
    public string PapelAtivo { get; set; } = "Cliente";
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiracao { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Oferta> Ofertas { get; set; } = new List<Oferta>();
    public ICollection<Solicitacao> SolicitacoesCliente { get; set; } = new List<Solicitacao>();
    public ICollection<Solicitacao> SolicitacoesPrestador { get; set; } = new List<Solicitacao>();
    public ICollection<Avaliacao> AvaliacoesFeitas { get; set; } = new List<Avaliacao>();
    public ICollection<Avaliacao> AvaliacoesRecebidas { get; set; } = new List<Avaliacao>();
    public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
    public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    public ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();
    public ICollection<Denuncia> DenunciasFeitas { get; set; } = new List<Denuncia>();
    public ICollection<Bloqueio> BloqueiosFeitos { get; set; } = new List<Bloqueio>();
    public ICollection<Bloqueio> BloqueiosSofridos { get; set; } = new List<Bloqueio>();
    public ICollection<Pagamento> PagamentosFeitos { get; set; } = new List<Pagamento>();
    public ICollection<Pagamento> PagamentosRecebidos { get; set; } = new List<Pagamento>();
}

