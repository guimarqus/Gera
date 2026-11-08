using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("Pagamentos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.IdentificadorTransacao)
            .HasMaxLength(200);

        builder.Property(p => p.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(p => p.Solicitacao)
            .WithMany(s => s.Pagamentos)
            .HasForeignKey(p => p.SolicitacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Pagador)
            .WithMany(u => u.PagamentosFeitos)
            .HasForeignKey(p => p.PagadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Recebedor)
            .WithMany(u => u.PagamentosRecebidos)
            .HasForeignKey(p => p.RecebedorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.SolicitacaoId, p.Status })
            .HasDatabaseName("IX_Pagamentos_SolicitacaoId_Status");
    }
}

