using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gera.Application.Auth;
using Gera.Application.Mensagens;
using Gera.Application.Solicitacoes;
using Gera.Domain.Entities;
using Gera.Domain.Enums;
using Gera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace Gera.IntegrationTests.Api;

public sealed class MensagensTests
{
    [Fact]
    public async Task ConversaPersisteMensagensProtegeAnexosEAtualizaLeitura()
    {
        using var factory = await TestApi.CreateAsync();
        using var cliente = factory.CreateClient();
        using var prestador = factory.CreateClient();
        using var terceiro = factory.CreateClient();
        await AutenticarAsync(cliente);
        var prestadorId = await AutenticarAsync(prestador);
        await AutenticarAsync(terceiro);
        int ofertaId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoriaId = await db.Categorias.Select(c => c.Id).FirstAsync();
            var oferta = new Oferta { PrestadorId = prestadorId, CategoriaId = categoriaId, Titulo = "Serviço para chat", Descricao = "Descrição de oferta para a conversa de integração.", Status = StatusOferta.Ativo };
            db.Ofertas.Add(oferta);
            await db.SaveChangesAsync();
            ofertaId = oferta.Id;
        }
        using var criar = await cliente.PostAsJsonAsync($"/api/ofertas/{ofertaId}/solicitacoes", new CriarSolicitacaoRequest("Preciso conversar com o profissional sobre este serviço."));
        var solicitacao = (await criar.Content.ReadFromJsonAsync<SolicitacaoDto>())!;
        var conversaUrl = $"/api/mensagens/{solicitacao.Id}";

        using var mensagem = await cliente.PostAsJsonAsync(conversaUrl, new EnviarMensagemRequest("Olá, gostaria de combinar os detalhes."));
        mensagem.EnsureSuccessStatusCode();
        (await terceiro.GetAsync(conversaUrl)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var conversas = await prestador.GetFromJsonAsync<ConversaDto[]>("/api/mensagens/conversas");
        conversas!.Should().ContainSingle(c => c.SolicitacaoId == solicitacao.Id && c.NaoLidas == 1);
        var historico = await prestador.GetFromJsonAsync<MensagemDto[]>(conversaUrl);
        historico!.Should().ContainSingle(m => m.Conteudo == "Olá, gostaria de combinar os detalhes." && !m.Lida);

        using var ler = await prestador.PostAsync($"{conversaUrl}/lidas", null);
        ler.EnsureSuccessStatusCode();
        (await prestador.GetFromJsonAsync<MensagemDto[]>(conversaUrl))!.Should().ContainSingle(m => m.Lida);

        var wav = new byte[32];
        "RIFF"u8.CopyTo(wav.AsSpan(0, 4));
        "WAVE"u8.CopyTo(wav.AsSpan(8, 4));
        using var form = new MultipartFormDataContent { { new ByteArrayContent(wav), "arquivo", "arquivo.exe" } };
        using var audio = await cliente.PostAsync($"{conversaUrl}/midia", form);
        audio.EnsureSuccessStatusCode();
        var anexo = (await audio.Content.ReadFromJsonAsync<MensagemDto>())!;
        anexo.Tipo.Should().Be(TipoMensagem.Audio);
        var mediaPath = anexo.MidiaUrl!;
        using var leituraMidia = await prestador.GetAsync(mediaPath);
        leituraMidia.StatusCode.Should().Be(HttpStatusCode.OK);
        leituraMidia.Content.Headers.ContentType!.MediaType.Should().Be("audio/wav");
        leituraMidia.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        (await terceiro.GetAsync(mediaPath)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var bitmap = new SKBitmap(20, 20);
        bitmap.Erase(SKColors.Orange);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        using var imageForm = new MultipartFormDataContent { { new ByteArrayContent(png.ToArray()), "arquivo", "foto.png" } };
        using var photo = await cliente.PostAsync($"{conversaUrl}/midia", imageForm);
        photo.EnsureSuccessStatusCode();
        var foto = (await photo.Content.ReadFromJsonAsync<MensagemDto>())!;
        foto.Tipo.Should().Be(TipoMensagem.Foto);
        using var fotoResposta = await cliente.GetAsync(foto.MidiaUrl);
        fotoResposta.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");

        using var falsoForm = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3, 4, 5]), "arquivo", "foto.png" } };
        using var falso = await cliente.PostAsync($"{conversaUrl}/midia", falsoForm);
        falso.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using (var scope = factory.Services.CreateScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<IArmazenamentoChat>();
            storage.Remover(mediaPath);
            storage.Remover(foto.MidiaUrl);
        }

        var notificacoesAntes = await prestador.GetFromJsonAsync<NotificacoesPaginaDto>("/api/notificacoes");
        var pref = await prestador.PutAsJsonAsync("/api/notificacoes/preferencias", new PreferenciasNotificacaoDto(false));
        pref.EnsureSuccessStatusCode();
        using var mensagemSemNotificacao = await cliente.PostAsJsonAsync(conversaUrl, new EnviarMensagemRequest("Outra mensagem sem alerta."));
        mensagemSemNotificacao.EnsureSuccessStatusCode();
        var notificacoes = await prestador.GetFromJsonAsync<NotificacoesPaginaDto>("/api/notificacoes");
        notificacoes!.Itens.Should().HaveCount(notificacoesAntes!.Itens.Count);
    }

    [Fact]
    public async Task HubExigeAutenticacao()
    {
        using var factory = await TestApi.CreateAsync();
        using var client = factory.CreateClient();
        using var resposta = await client.PostAsync("/hubs/chat/negotiate?negotiateVersion=1", null);
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AutenticarAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter;
        using var autenticada = await client.PostAsync($"/hubs/chat/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(token!)}", null);
        autenticada.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<int> AutenticarAsync(HttpClient client)
    {
        var registration = TestApi.NewRegistration(Guid.NewGuid().ToString("N")[..10]);
        using var response = await client.PostAsJsonAsync("/api/auth/register", registration);
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<PublicAuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (await client.GetFromJsonAsync<UserResponse>("/api/auth/eu"))!.Id;
    }
}
