using Gera.Domain.Exceptions;
using Gera.Infrastructure.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace Gera.IntegrationTests.Api;

public sealed class ArmazenamentoMidiaTests
{
    [Fact]
    public async Task ReprocessaImagemELimitaDimensoesDaFotoEDaMiniatura()
    {
        var pasta = Directory.CreateTempSubdirectory("gera-midia-");
        try
        {
            var storage = CriarArmazenamento(pasta.FullName);
            using var bitmap = new SKBitmap(1700, 850);
            bitmap.Erase(SKColors.Teal);
            using var imagem = SKImage.FromBitmap(bitmap);
            using var entrada = imagem.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = entrada.AsStream();

            var arquivos = await storage.SalvarFotoAsync(42, stream, CancellationToken.None);
            using var principal = storage.AbrirFoto(42, Path.GetFileName(arquivos.Url));
            using var miniatura = storage.AbrirFoto(42, Path.GetFileName(arquivos.UrlMiniatura));
            Assert.NotNull(principal);
            Assert.NotNull(miniatura);
            using var fotoCodec = SKCodec.Create(principal);
            using var miniaturaCodec = SKCodec.Create(miniatura);
            Assert.NotNull(fotoCodec);
            Assert.NotNull(miniaturaCodec);

            Assert.Equal(SKEncodedImageFormat.Webp, fotoCodec.EncodedFormat);
            Assert.Equal((1600, 800), (fotoCodec.Info.Width, fotoCodec.Info.Height));
            Assert.Equal((360, 180), (miniaturaCodec.Info.Width, miniaturaCodec.Info.Height));
            Assert.Null(storage.AbrirFoto(43, Path.GetFileName(arquivos.Url)));
            Assert.Null(storage.AbrirFoto(42, "../fora.webp"));
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task RejeitaConteudoInvalidoSemGravarArquivo()
    {
        var pasta = Directory.CreateTempSubdirectory("gera-midia-");
        try
        {
            var storage = CriarArmazenamento(pasta.FullName);
            using var invalido = new MemoryStream("este arquivo não é uma foto"u8.ToArray());

            await Assert.ThrowsAsync<DomainException>(() => storage.SalvarFotoAsync(42, invalido, CancellationToken.None));
            Assert.Empty(pasta.EnumerateFiles("*", SearchOption.AllDirectories));
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    private static ArmazenamentoLocalMidia CriarArmazenamento(string pasta) =>
        new(new AmbienteTeste { ContentRootPath = pasta }, NullLogger<ArmazenamentoLocalMidia>.Instance);

    private sealed class AmbienteTeste : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Gera";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
