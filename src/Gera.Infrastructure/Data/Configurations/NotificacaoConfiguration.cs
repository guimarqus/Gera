using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("Notificacoes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Titulo)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(n => n.Mensagem)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Link)
            .HasMaxLength(300);

        builder.Property(n => n.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(n => n.Usuario)
            .WithMany(u => u.Notificacoes)
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice especificado na Seção 10
        builder.HasIndex(n => new { n.UsuarioId, n.Lida })
            .HasDatabaseName("IX_Notificacoes_UsuarioId_Lida");
    }
}

