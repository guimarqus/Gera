using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class OfertaConfiguration : IEntityTypeConfiguration<Oferta>
{
    public void Configure(EntityTypeBuilder<Oferta> builder)
    {
        builder.ToTable("Ofertas");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Titulo)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(o => o.Descricao)
            .IsRequired()
            .HasMaxLength(2000);

        // Coluna geography no SQL Server para NetTopologySuite Point
        builder.Property(o => o.Localizacao)
            .HasColumnType("geography")
            .IsRequired();

        builder.Property(o => o.AreaAtendida)
            .HasMaxLength(150);

        builder.Property(o => o.Disponibilidade)
            .HasMaxLength(200);

        builder.Property(o => o.FormasPagamento)
            .HasMaxLength(200);

        builder.Property(o => o.Valor)
            .HasPrecision(18, 2);

        builder.Property(o => o.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(o => o.AtualizadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relacionamentos
        builder.HasOne(o => o.Prestador)
            .WithMany(u => u.Ofertas)
            .HasForeignKey(o => o.PrestadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Categoria)
            .WithMany(c => c.Ofertas)
            .HasForeignKey(o => o.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices especificados na Seção 10
        builder.HasIndex(o => new { o.CategoriaId, o.Status })
            .HasDatabaseName("IX_Ofertas_CategoriaId_Status");

        // Índice espacial em Ofertas.Localizacao
        builder.HasIndex(o => o.Localizacao)
            .HasDatabaseName("SPATIAL_IX_Ofertas_Localizacao");
    }
}

