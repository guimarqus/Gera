using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gera.Infrastructure.Data.Configurations;

public sealed class AuditoriaAdminConfiguration : IEntityTypeConfiguration<AuditoriaAdmin>
{
    public void Configure(EntityTypeBuilder<AuditoriaAdmin> builder)
    {
        builder.ToTable("AuditoriasAdmin");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Acao).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Entidade).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Identificador).IsRequired().HasMaxLength(128);
        builder.Property(a => a.Motivo).HasMaxLength(1000);
        builder.Property(a => a.CriadoEm).HasDefaultValueSql("GETUTCDATE()");
        builder.HasOne(a => a.Administrador).WithMany().HasForeignKey(a => a.AdministradorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.AdministradorId, a.CriadoEm }).HasDatabaseName("IX_AuditoriasAdmin_AdministradorId_CriadoEm");
    }
}
