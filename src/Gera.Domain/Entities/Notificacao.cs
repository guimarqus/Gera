using Gera.Domain.Enums;

namespace Gera.Domain.Entities;

public class Notificacao
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public TipoNotificacao Tipo { get; set; } = TipoNotificacao.Sistema;

    public int? ReferenciaId { get; set; }
    public string? Link { get; set; }

    public bool Lida { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

