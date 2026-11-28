using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Gera.Application.Diagnostico;
using Gera.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Gera.UnitTests.Diagnostico;

public sealed class OpenAiDiagnosticoProviderTests
{
    [Fact]
    public async Task EnviaDescricaoSomenteNoInputELeRespostaEstruturada()
    {
        string? sentBody = null;
        string? authScheme = null;
        string? authParameter = null;
        var handler = new TestHandler(async (message, ct) =>
        {
            sentBody = await message.Content!.ReadAsStringAsync(ct);
            authScheme = message.Headers.Authorization?.Scheme;
            authParameter = message.Headers.Authorization?.Parameter;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"status":"completed","output":[{"type":"message","content":[{"type":"output_text","text":"{\"resumo\":\"Pode ser necessário um encanador.\",\"categoriaIds\":[3],\"sequenciaSugerida\":[\"Feche o registro de água.\"]}"}]}]}
                    """, Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        var provider = new OpenAiDiagnosticoProvider(client,
            Options.Create(new OpenAiDiagnosticoOptions { Enabled = true, ApiKey = "secret", Model = "test-model" }),
            NullLogger<OpenAiDiagnosticoProvider>.Instance);

        var result = await provider.AnalisarAsync("Torneira pingando", [new CategoriaDisponivel(3, "Hidráulica")], default);

        Assert.Equal("Pode ser necessário um encanador.", result.Resumo);
        Assert.Equal(new[] { 3 }, result.CategoriaIds);
        Assert.Equal("Feche o registro de água.", Assert.Single(result.SequenciaSugerida));
        Assert.Equal("Bearer", authScheme);
        Assert.Equal("secret", authParameter);
        using var body = System.Text.Json.JsonDocument.Parse(sentBody!);
        Assert.False(body.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal("test-model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("json_schema", body.RootElement.GetProperty("text").GetProperty("format").GetProperty("type").GetString());
        Assert.DoesNotContain("secret", sentBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejeitaRespostaSemTextoEstruturado()
    {
        var handler = new TestHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":\"incomplete\",\"output\":[]}", Encoding.UTF8, "application/json")
        }));
        var provider = new OpenAiDiagnosticoProvider(new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") },
            Options.Create(new OpenAiDiagnosticoOptions { Enabled = true, ApiKey = "secret", Model = "test-model" }),
            NullLogger<OpenAiDiagnosticoProvider>.Instance);

        await Assert.ThrowsAsync<DiagnosticoProviderException>(() => provider.AnalisarAsync("problema", [], default));
    }

    private sealed class TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
