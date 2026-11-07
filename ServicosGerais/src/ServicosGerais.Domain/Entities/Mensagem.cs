using ServicosGerais.Domain.Enums;

namespace ServicosGerais.Domain.Entities;

public class Mensagem
{
    public long Id { get; set; }
    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;
    public int RemetenteId { get; set; }
    public Usuario Remetente { get; set; } = null!;
    public TipoMensagem Tipo { get; set; } = TipoMensagem.Texto;
    public string Conteudo { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public bool Lida { get; set; }
}

