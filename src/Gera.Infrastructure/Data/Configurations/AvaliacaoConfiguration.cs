using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class AvaliacaoConfiguration : IEntityTypeConfiguration<Avaliacao>
{
    public void Configure(EntityTypeBuilder<Avaliacao> builder)
    {
        builder.ToTable("Avaliacoes", t =>
        {
            // CHECK constraint Nota 1 a 5 conforme Seção 10
            t.HasCheckConstraint("CK_Avaliacoes_Nota", "[Nota] >= 1 AND [Nota] <= 5");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Comentario)
            .HasMaxLength(1000);

        builder.Property(a => a.Resposta)
            .HasMaxLength(1000);

        builder.Property(a => a.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(a => a.Solicitacao)
            .WithMany(s => s.Avaliacoes)
            .HasForeignKey(a => a.SolicitacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Avaliador)
            .WithMany(u => u.AvaliacoesFeitas)
            .HasForeignKey(a => a.AvaliadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Avaliado)
            .WithMany(u => u.AvaliacoesRecebidas)
            .HasForeignKey(a => a.AvaliadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.SolicitacaoId, a.AvaliadorId })
            .IsUnique()
            .HasDatabaseName("IX_Avaliacoes_SolicitacaoId_AvaliadorId");

        builder.HasIndex(a => a.AvaliadoId)
            .HasDatabaseName("IX_Avaliacoes_AvaliadoId");
    }
}

