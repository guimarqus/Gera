using Microsoft.EntityFrameworkCore;
using ServicosGerais.Domain.Entities;

namespace ServicosGerais.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Categoria> Categorias { get; }
    DbSet<Oferta> Ofertas { get; }
    DbSet<OfertaFoto> OfertaFotos { get; }
    DbSet<OfertaTag> OfertaTags { get; }
    DbSet<Solicitacao> Solicitacoes { get; }
    DbSet<SolicitacaoHistorico> SolicitacaoHistoricos { get; }
    DbSet<Mensagem> Mensagens { get; }
    DbSet<Avaliacao> Avaliacoes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

