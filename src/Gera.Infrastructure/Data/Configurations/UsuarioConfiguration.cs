using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.FotoUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Biografia)
            .HasMaxLength(1000);

        builder.Property(u => u.Documento)
            .HasMaxLength(20);

        builder.Property(u => u.PapelAtivo)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Cliente");

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(200);

        builder.Property(u => u.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.AtualizadoEm)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}

