namespace Gera.Application.Mensagens;

public sealed record MidiaChatDto(string Url, string ContentType);

public interface IArmazenamentoChat
{
    Task<MidiaChatDto> SalvarAsync(Stream conteudo, long tamanho, CancellationToken ct);
    (Stream Arquivo, string ContentType)? Abrir(Guid id);
    void Remover(string? url);
}
