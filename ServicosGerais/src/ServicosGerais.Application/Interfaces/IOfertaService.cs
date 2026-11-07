using ServicosGerais.Application.DTOs.Ofertas;

namespace ServicosGerais.Application.Interfaces;

public interface IOfertaService
{
    Task<OfertaDetalheDto?> ObterPorIdAsync(int id);
    Task<List<OfertaCardDto>> ObterMinhasAsync(int prestadorId);
    Task<OfertaDetalheDto> CriarAsync(int prestadorId, CriarOfertaDto dto);
    Task<OfertaDetalheDto> AtualizarAsync(int id, int prestadorId, AtualizarOfertaDto dto);
    Task AlterarStatusAsync(int id, int prestadorId, string status);
}

