using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class MensagemConfiguration : IEntityTypeConfiguration<Mensagem>
{
    public void Configure(EntityTypeBuilder<Mensagem> builder)
    {
        builder.ToTable("Mensagens");

        // Chave primária bigint (long) conforme Seção 10
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Conteudo)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.MidiaUrl)
            .HasMaxLength(500);

        builder.Property(m => m.DataHora)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(m => m.Solicitacao)
            .WithMany(s => s.Mensagens)
            .HasForeignKey(m => m.SolicitacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Remetente)
            .WithMany(u => u.Mensagens)
            .HasForeignKey(m => m.RemetenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Orcamento)
            .WithMany()
            .HasForeignKey(m => m.OrcamentoId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índice especificado na Seção 10
        builder.HasIndex(m => new { m.SolicitacaoId, m.DataHora })
            .HasDatabaseName("IX_Mensagens_SolicitacaoId_DataHora");
    }
}

