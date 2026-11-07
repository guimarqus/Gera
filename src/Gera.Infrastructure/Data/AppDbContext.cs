using Gera.Application.Interfaces;
using Gera.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gera.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Users;
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Oferta> Ofertas => Set<Oferta>();
    public DbSet<OfertaFoto> OfertaFotos => Set<OfertaFoto>();
    public DbSet<OfertaTag> OfertaTags => Set<OfertaTag>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<Solicitacao> Solicitacoes => Set<Solicitacao>();
    public DbSet<SolicitacaoHistorico> SolicitacaoHistoricos => Set<SolicitacaoHistorico>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Denuncia> Denuncias => Set<Denuncia>();
    public DbSet<Bloqueio> Bloqueios => Set<Bloqueio>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Nomenclatura em português no plural para tabelas de Identity
        builder.Entity<Usuario>().ToTable("Usuarios");
        builder.Entity<IdentityRole<int>>().ToTable("Papeis");
        builder.Entity<IdentityUserRole<int>>().ToTable("UsuarioPapeis");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UsuarioClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UsuarioLogins");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("PapelClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UsuarioTokens");

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

