using Gera.Application.Mensagens;
using Gera.Domain.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Gera.Infrastructure.Services;

public sealed partial class ArmazenamentoLocalChat(IHostEnvironment environment, ILogger<ArmazenamentoLocalChat> logger) : IArmazenamentoChat
{
    public const long MaximoFotoBytes = 5 * 1024 * 1024;
    public const long MaximoAudioBytes = 10 * 1024 * 1024;
    private const long MaximoPixels = 16_000_000;
    private readonly string _pasta = Path.Combine(environment.ContentRootPath, "App_Data", "mensagens");
    private readonly SemaphoreSlim _limite = new(2);

    public async Task<MidiaChatDto> SalvarAsync(Stream conteudo, long tamanho, CancellationToken ct)
    {
        if (!conteudo.CanSeek || tamanho <= 0) throw new DomainException("O arquivo enviado está vazio ou inválido.");
        var header = new byte[Math.Min(12, (int)tamanho)];
        await conteudo.ReadExactlyAsync(header, ct);
        conteudo.Position = 0;
        if (EhImagem(header))
        {
            if (tamanho > MaximoFotoBytes) throw new DomainException("A foto deve ter até 5 MB.");
            return await SalvarImagemAsync(conteudo, ct);
        }
        if (tamanho > MaximoAudioBytes) throw new DomainException("O áudio deve ter até 10 MB.");
        var (extensao, contentType) = TipoAudio(header);
        if (extensao is null) throw new DomainException("Envie uma foto JPEG, PNG ou WebP, ou áudio MP3, WAV, OGG, WebM ou M4A.");
        var id = Guid.NewGuid();
        Directory.CreateDirectory(_pasta);
        var destino = Caminho(id, extensao);
        try
        {
            await using var output = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await conteudo.CopyToAsync(output, ct);
            return new MidiaChatDto($"/api/mensagens/midia/{id:N}", contentType);
        }
        catch { Remover($"/api/mensagens/midia/{id:N}"); throw; }
    }

    public (Stream Arquivo, string ContentType)? Abrir(Guid id)
    {
        foreach (var (ext, type) in TiposPermitidos())
        {
            try { return (new FileStream(Caminho(id, ext), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete), type); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return null;
    }

    public void Remover(string? url)
    {
        if (url is null) return;
        var nome = url[(url.LastIndexOf('/') + 1)..];
        if (!Guid.TryParseExact(nome, "N", out var id)) return;
        foreach (var (ext, _) in TiposPermitidos())
            try { File.Delete(Caminho(id, ext)); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { logger.LogWarning(e, "Não foi possível remover mídia do chat."); }
    }

    private async Task<MidiaChatDto> SalvarImagemAsync(Stream input, CancellationToken ct)
    {
        await _limite.WaitAsync(ct);
        var id = Guid.NewGuid();
        var destino = Caminho(id, ".webp");
        try
        {
            using var codec = SKCodec.Create(input);
            if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp) ||
                codec.FrameCount > 1 || codec.Info.Width <= 0 || codec.Info.Height <= 0 || codec.Info.Width > 8192 || codec.Info.Height > 8192 ||
                (long)codec.Info.Width * codec.Info.Height > MaximoPixels)
                throw new DomainException("A foto é inválida ou excede 16 megapixels.");
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success) throw new DomainException("A foto está corrompida.");
            using var image = SKImage.FromBitmap(bitmap);
            var escala = Math.Min(1d, 1600d / Math.Max(image.Width, image.Height));
            var width = Math.Max(1, (int)Math.Round(image.Width * escala));
            var height = Math.Max(1, (int)Math.Round(image.Height * escala));
            using var surface = SKSurface.Create(new SKImageInfo(width, height)) ?? throw new InvalidOperationException("Não foi possível processar a foto.");
            surface.Canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear));
            using var resized = surface.Snapshot();
            using var encoded = resized.Encode(SKEncodedImageFormat.Webp, 84) ?? throw new InvalidOperationException("Não foi possível converter a foto.");
            Directory.CreateDirectory(_pasta);
            await using var output = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await using var data = encoded.AsStream();
            await data.CopyToAsync(output, ct);
            return new MidiaChatDto($"/api/mensagens/midia/{id:N}", "image/webp");
        }
        catch { Remover($"/api/mensagens/midia/{id:N}"); throw; }
        finally { _limite.Release(); }
    }

    private string Caminho(Guid id, string ext) => Path.Combine(_pasta, id.ToString("N") + ext);
    private static bool EhImagem(byte[] h) => h.Length >= 12 && (h[0] == 0xff && h[1] == 0xd8 && h[2] == 0xff ||
        h.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) || h.AsSpan(0, 4).SequenceEqual("RIFF"u8) && h.AsSpan(8, 4).SequenceEqual("WEBP"u8));
    private static (string? Extensao, string ContentType) TipoAudio(byte[] h)
    {
        if (h.Length >= 12 && h.AsSpan(0, 4).SequenceEqual("RIFF"u8) && h.AsSpan(8, 4).SequenceEqual("WAVE"u8)) return (".wav", "audio/wav");
        if (h.Length >= 4 && h.AsSpan(0, 4).SequenceEqual("OggS"u8)) return (".ogg", "audio/ogg");
        if (h.Length >= 3 && h.AsSpan(0, 3).SequenceEqual("ID3"u8) || h.Length >= 2 && h[0] == 0xff && (h[1] & 0xe0) == 0xe0) return (".mp3", "audio/mpeg");
        if (h.Length >= 4 && h.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 })) return (".webm", "audio/webm");
        if (h.Length >= 12 && h.AsSpan(4, 4).SequenceEqual("ftyp"u8) &&
            (h.AsSpan(8, 4).SequenceEqual("M4A "u8) || h.AsSpan(8, 4).SequenceEqual("M4B "u8) || h.AsSpan(8, 4).SequenceEqual("M4P "u8)))
            return (".m4a", "audio/mp4");
        return (null, "application/octet-stream");
    }

    private static (string Extensao, string ContentType)[] TiposPermitidos() => [(".webp", "image/webp"), (".wav", "audio/wav"), (".ogg", "audio/ogg"), (".mp3", "audio/mpeg"), (".webm", "audio/webm"), (".m4a", "audio/mp4")];
}
