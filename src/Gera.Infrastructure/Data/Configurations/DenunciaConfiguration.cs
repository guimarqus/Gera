using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class DenunciaConfiguration : IEntityTypeConfiguration<Denuncia>
{
    public void Configure(EntityTypeBuilder<Denuncia> builder)
    {
        builder.ToTable("Denuncias");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Motivo)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(d => d.Descricao)
            .HasMaxLength(1000);

        builder.Property(d => d.ParecerModerador)
            .HasMaxLength(1000);

        builder.Property(d => d.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(d => d.Denunciante)
            .WithMany(u => u.DenunciasFeitas)
            .HasForeignKey(d => d.DenuncianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.Status, d.CriadoEm })
            .HasDatabaseName("IX_Denuncias_Status_CriadoEm");
    }
}

