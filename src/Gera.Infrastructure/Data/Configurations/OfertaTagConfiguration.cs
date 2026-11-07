using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public class OfertaTagConfiguration : IEntityTypeConfiguration<OfertaTag>
{
    public void Configure(EntityTypeBuilder<OfertaTag> builder)
    {
        builder.ToTable("OfertaTags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Tag)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(t => t.Oferta)
            .WithMany(o => o.Tags)
            .HasForeignKey(t => t.OfertaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Tag);
    }
}

