using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class BloqueioConfiguration : IEntityTypeConfiguration<Bloqueio>
{
    public void Configure(EntityTypeBuilder<Bloqueio> builder)
    {
        builder.ToTable("Bloqueios");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Motivo)
            .HasMaxLength(300);

        builder.Property(b => b.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(b => b.Bloqueador)
            .WithMany(u => u.BloqueiosFeitos)
            .HasForeignKey(b => b.BloqueadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Bloqueado)
            .WithMany(u => u.BloqueiosSofridos)
            .HasForeignKey(b => b.BloqueadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.BloqueadorId, b.BloqueadoId })
            .IsUnique()
            .HasDatabaseName("IX_Bloqueios_BloqueadorId_BloqueadoId");
    }
}

