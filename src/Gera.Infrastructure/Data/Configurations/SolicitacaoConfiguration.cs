using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class SolicitacaoConfiguration : IEntityTypeConfiguration<Solicitacao>
{
    public void Configure(EntityTypeBuilder<Solicitacao> builder)
    {
        builder.ToTable("Solicitacoes");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Descricao)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(s => s.Valor)
            .HasPrecision(18, 2);

        builder.Property(s => s.MotivoCancelamento)
            .HasMaxLength(500);

        // Concorrência otimista via RowVersion / timestamp SQL Server
        builder.Property(s => s.RowVersion)
            .IsRowVersion();

        builder.Property(s => s.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(s => s.AtualizadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relacionamentos
        builder.HasOne(s => s.Cliente)
            .WithMany(u => u.SolicitacoesCliente)
            .HasForeignKey(s => s.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Prestador)
            .WithMany(u => u.SolicitacoesPrestador)
            .HasForeignKey(s => s.PrestadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Oferta)
            .WithMany(o => o.Solicitacoes)
            .HasForeignKey(s => s.OfertaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices especificados na Seção 10
        builder.HasIndex(s => new { s.ClienteId, s.Etapa })
            .HasDatabaseName("IX_Solicitacoes_ClienteId_Etapa");

        builder.HasIndex(s => new { s.PrestadorId, s.Etapa })
            .HasDatabaseName("IX_Solicitacoes_PrestadorId_Etapa");
    }
}

