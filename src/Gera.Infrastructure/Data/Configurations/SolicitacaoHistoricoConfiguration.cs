using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class SolicitacaoHistoricoConfiguration : IEntityTypeConfiguration<SolicitacaoHistorico>
{
    public void Configure(EntityTypeBuilder<SolicitacaoHistorico> builder)
    {
        builder.ToTable("SolicitacaoHistoricos");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Observacao)
            .HasMaxLength(500);

        builder.Property(h => h.OcorreuEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(h => h.Solicitacao)
            .WithMany(s => s.Historicos)
            .HasForeignKey(h => h.SolicitacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.SolicitacaoId, h.OcorreuEm })
            .HasDatabaseName("IX_SolicitacaoHistoricos_SolicitacaoId_OcorreuEm");
    }
}

