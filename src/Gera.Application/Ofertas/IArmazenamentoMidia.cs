namespace Gera.Application.Ofertas;

public sealed record MidiaOferta(string Url, string UrlMiniatura);

public interface IArmazenamentoMidia
{
    Task<MidiaOferta> SalvarFotoAsync(int ofertaId, Stream arquivo, CancellationToken cancellationToken);
    Stream? AbrirFoto(int ofertaId, string nome);
    void RemoverFoto(int ofertaId, string? url);
}
