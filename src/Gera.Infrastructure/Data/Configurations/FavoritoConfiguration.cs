using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class FavoritoConfiguration : IEntityTypeConfiguration<Favorito>
{
    public void Configure(EntityTypeBuilder<Favorito> builder)
    {
        builder.ToTable("Favoritos");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(f => f.Usuario)
            .WithMany(u => u.Favoritos)
            .HasForeignKey(f => f.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Oferta)
            .WithMany(o => o.Favoritos)
            .HasForeignKey(f => f.OfertaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.UsuarioId, f.OfertaId })
            .IsUnique()
            .HasDatabaseName("IX_Favoritos_UsuarioId_OfertaId");
    }
}

