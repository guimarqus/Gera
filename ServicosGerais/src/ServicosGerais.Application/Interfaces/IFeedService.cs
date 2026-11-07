using ServicosGerais.Application.DTOs.Feed;

namespace ServicosGerais.Application.Interfaces;

public interface IFeedService
{
    Task<FeedResultDto> ObterFeedAsync(FeedQueryDto query);
}

