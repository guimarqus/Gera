using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class OfertaFotoConfiguration : IEntityTypeConfiguration<OfertaFoto>
{
    public void Configure(EntityTypeBuilder<OfertaFoto> builder)
    {
        builder.ToTable("OfertaFotos");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.CriadoEm)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(f => f.Oferta)
            .WithMany(o => o.Fotos)
            .HasForeignKey(f => f.OfertaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

