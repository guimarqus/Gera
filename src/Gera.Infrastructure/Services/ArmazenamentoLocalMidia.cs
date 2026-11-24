using System.Text.RegularExpressions;
using Gera.Application.Ofertas;
using Gera.Domain.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Gera.Infrastructure.Services;

public sealed partial class ArmazenamentoLocalMidia(IHostEnvironment ambiente, ILogger<ArmazenamentoLocalMidia> logger)
    : IArmazenamentoMidia
{
    private const int MaximoPixels = 16_000_000;
    private const int MaximaDimensaoEntrada = 8192;
    private readonly string _raiz = Path.Combine(ambiente.ContentRootPath, "App_Data", "ofertas");
    private readonly SemaphoreSlim _processamento = new(2);

    public async Task<MidiaOferta> SalvarFotoAsync(int ofertaId, Stream arquivo, CancellationToken cancellationToken)
    {
        await _processamento.WaitAsync(cancellationToken);
        try
        {
            var identificador = Guid.NewGuid().ToString("N");
            var nome = $"{identificador}.webp";
            var miniatura = $"{identificador}-mini.webp";
            var pasta = Pasta(ofertaId);
            using var imagem = Decodificar(arquivo, out var orientacao);
            using var orientada = Orientar(imagem, orientacao);
            Directory.CreateDirectory(pasta);
            var url = Url(ofertaId, nome);
            var urlMiniatura = Url(ofertaId, miniatura);
            try
            {
                await GravarAsync(orientada, 1600, 84, Path.Combine(pasta, nome), cancellationToken);
                await GravarAsync(orientada, 360, 76, Path.Combine(pasta, miniatura), cancellationToken);
                return new MidiaOferta(url, urlMiniatura);
            }
            catch
            {
                RemoverFoto(ofertaId, url);
                RemoverFoto(ofertaId, urlMiniatura);
                throw;
            }
        }
        finally
        {
            _processamento.Release();
        }
    }

    public Stream? AbrirFoto(int ofertaId, string nome)
    {
        if (!NomeGerado().IsMatch(nome))
            return null;

        try
        {
            return new FileStream(Path.Combine(Pasta(ofertaId), nome), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public void RemoverFoto(int ofertaId, string? url)
    {
        var prefixo = Url(ofertaId, string.Empty);
        if (url is null || !url.StartsWith(prefixo, StringComparison.Ordinal))
            return;
        var nome = url[prefixo.Length..];
        if (!NomeGerado().IsMatch(nome))
            return;

        try
        {
            File.Delete(Path.Combine(Pasta(ofertaId), nome));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Não foi possível remover um arquivo de mídia da oferta {OfertaId}.", ofertaId);
        }
    }

    private static SKBitmap Decodificar(Stream arquivo, out SKEncodedOrigin orientacao)
    {
        Span<byte> cabecalho = stackalloc byte[12];
        if (!arquivo.CanSeek || arquivo.Length < cabecalho.Length || arquivo.Length > OfertaFotosService.MaximoBytes)
            throw new DomainException("Envie uma imagem JPEG, PNG ou WebP de até 5 MB.");

        arquivo.ReadExactly(cabecalho);
        arquivo.Position = 0;
        var jpeg = cabecalho[0] == 0xff && cabecalho[1] == 0xd8 && cabecalho[2] == 0xff;
        var png = cabecalho[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var webp = cabecalho[..4].SequenceEqual("RIFF"u8) && cabecalho[8..].SequenceEqual("WEBP"u8);
        if (!jpeg && !png && !webp)
            throw new DomainException("Envie uma imagem JPEG, PNG ou WebP.");

        using var codec = SKCodec.Create(arquivo);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new DomainException("A imagem enviada é inválida.");
        var largura = codec.Info.Width;
        var altura = codec.Info.Height;
        if (largura is <= 0 or > MaximaDimensaoEntrada || altura is <= 0 or > MaximaDimensaoEntrada ||
            (long)largura * altura > MaximoPixels || codec.FrameCount > 1)
            throw new DomainException("Envie uma imagem estática com até 16 megapixels e lado máximo de 8.192 pixels.");

        var info = new SKImageInfo(largura, altura, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        try
        {
            var pixels = bitmap.GetPixels();
            if (pixels == IntPtr.Zero)
                throw new InvalidOperationException("Não foi possível alocar a imagem.");
            if (codec.GetPixels(info, pixels) != SKCodecResult.Success)
                throw new DomainException("A imagem está corrompida ou incompleta.");
            orientacao = codec.EncodedOrigin;
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static SKImage Orientar(SKBitmap bitmap, SKEncodedOrigin orientacao)
    {
        var w = bitmap.Width;
        var h = bitmap.Height;
        var (matriz, largura, altura) = orientacao switch
        {
            SKEncodedOrigin.TopRight => (new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1), w, h),
            SKEncodedOrigin.BottomRight => (new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), w, h),
            SKEncodedOrigin.BottomLeft => (new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1), w, h),
            SKEncodedOrigin.LeftTop => (new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1), h, w),
            SKEncodedOrigin.RightTop => (new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1), h, w),
            SKEncodedOrigin.RightBottom => (new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), h, w),
            SKEncodedOrigin.LeftBottom => (new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1), h, w),
            _ => (SKMatrix.Identity, w, h)
        };
        using var superficie = SKSurface.Create(new SKImageInfo(largura, altura))
            ?? throw new InvalidOperationException("Não foi possível preparar a imagem.");
        using var imagem = SKImage.FromBitmap(bitmap);
        superficie.Canvas.Clear(SKColors.Transparent);
        superficie.Canvas.SetMatrix(matriz);
        superficie.Canvas.DrawImage(imagem, 0, 0);
        return superficie.Snapshot();
    }

    private static async Task GravarAsync(SKImage fonte, int limite, int qualidade, string destino, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var escala = Math.Min(1d, (double)limite / Math.Max(fonte.Width, fonte.Height));
        var largura = Math.Max(1, (int)Math.Round(fonte.Width * escala));
        var altura = Math.Max(1, (int)Math.Round(fonte.Height * escala));
        using var superficie = SKSurface.Create(new SKImageInfo(largura, altura))
            ?? throw new InvalidOperationException("Não foi possível redimensionar a imagem.");
        superficie.Canvas.Clear(SKColors.Transparent);
        superficie.Canvas.DrawImage(fonte, new SKRect(0, 0, largura, altura), new SKSamplingOptions(SKFilterMode.Linear));
        using var redimensionada = superficie.Snapshot();
        using var codificada = redimensionada.Encode(SKEncodedImageFormat.Webp, qualidade)
            ?? throw new InvalidOperationException("Não foi possível codificar a imagem.");
        using var dados = codificada.AsStream();
        await using var saida = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, useAsync: true);
        await dados.CopyToAsync(saida, ct);
    }

    private string Pasta(int ofertaId) => Path.Combine(_raiz, ofertaId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static string Url(int ofertaId, string nome) => $"/api/ofertas/{ofertaId}/midia/{nome}";

    [GeneratedRegex(@"\A[0-9a-f]{32}(?:-mini)?\.webp\z", RegexOptions.CultureInvariant)]
    private static partial Regex NomeGerado();
}
