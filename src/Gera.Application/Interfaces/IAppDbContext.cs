using Gera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Gera.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Categoria> Categorias { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Oferta> Ofertas { get; }
    DbSet<OfertaFoto> OfertaFotos { get; }
    DbSet<OfertaTag> OfertaTags { get; }
    DbSet<Favorito> Favoritos { get; }
    DbSet<Solicitacao> Solicitacoes { get; }
    DbSet<SolicitacaoHistorico> SolicitacaoHistoricos { get; }
    DbSet<Orcamento> Orcamentos { get; }
    DbSet<Mensagem> Mensagens { get; }
    DbSet<Avaliacao> Avaliacoes { get; }
    DbSet<Notificacao> Notificacoes { get; }
    DbSet<Denuncia> Denuncias { get; }
    DbSet<Bloqueio> Bloqueios { get; }
    DbSet<Pagamento> Pagamentos { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

