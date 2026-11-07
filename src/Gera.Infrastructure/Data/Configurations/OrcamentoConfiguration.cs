using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("Orcamentos");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.OQueEstaIncluso)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(o => o.Observacoes)
            .HasMaxLength(1000);

        builder.Property(o => o.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(o => o.Solicitacao)
            .WithMany(s => s.Orcamentos)
            .HasForeignKey(o => o.SolicitacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Prestador)
            .WithMany()
            .HasForeignKey(o => o.PrestadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.SolicitacaoId, o.Status })
            .HasDatabaseName("IX_Orcamentos_SolicitacaoId_Status");
    }
}

