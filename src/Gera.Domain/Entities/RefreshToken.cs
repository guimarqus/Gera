namespace Gera.Domain.Entities;

public sealed class RefreshToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public string? SubstituidoPorHash { get; set; }
    public string? IpCriacao { get; set; }
    public Usuario? Usuario { get; set; }
}
