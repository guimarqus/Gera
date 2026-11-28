namespace Gera.Domain.Entities;

public class AuditoriaAdmin
{
    public long Id { get; set; }
    public int AdministradorId { get; set; }
    public Usuario Administrador { get; set; } = null!;
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public string Identificador { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
