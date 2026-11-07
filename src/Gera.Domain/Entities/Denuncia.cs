using Gera.Domain.Enums;

namespace Gera.Domain.Entities;

public class Denuncia
{
    public int Id { get; set; }

    public int DenuncianteId { get; set; }
    public Usuario Denunciante { get; set; } = null!;

    public TipoAlvoDenuncia TipoAlvo { get; set; }
    public int AlvoId { get; set; }

    public string Motivo { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public StatusDenuncia Status { get; set; } = StatusDenuncia.Pendente;
    public string? ParecerModerador { get; set; }
    public int? ModeradorId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvidoEm { get; set; }
}

