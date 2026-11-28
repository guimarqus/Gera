using Gera.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Gera.UnitTests.Auth;

public sealed class SmtpEmailOptionsTests
{
    [Fact]
    public void ConfiguracaoCompletaComTlsEValida()
    {
        var options = new SmtpEmailOptions { Host = "smtp.example.test", Port = 587, EnableSsl = true, UserName = "gera", Password = "secret-from-config", FromAddress = "no-reply@example.test", FromName = "Gera" };
        Assert.True(options.IsValid());
    }

    [Theory]
    [InlineData("", 587, true, "no-reply@example.test", "", "")]
    [InlineData("smtp.example.test", 0, true, "no-reply@example.test", "", "")]
    [InlineData("smtp.example.test", 587, false, "no-reply@example.test", "", "")]
    [InlineData("smtp.example.test", 587, true, "invalid-address", "", "")]
    [InlineData("smtp.example.test", 587, true, "no-reply@example.test", "gera", "")]
    public void RejeitaConfiguracaoIncompletaOuSemSeguranca(string host, int port, bool tls, string from, string user, string password)
    {
        var options = new SmtpEmailOptions { Host = host, Port = port, EnableSsl = tls, FromAddress = from, FromName = "Gera", UserName = user, Password = password };
        Assert.False(options.IsValid());
    }

    [Fact]
    public async Task FallbackDeDesenvolvimentoNaoRegistraEmailNemToken()
    {
        var logger = new CapturingLogger<DevelopmentEmailSender>();
        var sender = new DevelopmentEmailSender(logger);
        const string email = "person@example.test";
        const string token = "private-reset-token";
        await sender.SendPasswordResetAsync(email, $"https://localhost/reset?token={token}");
        var output = string.Join(" ", logger.Messages);
        Assert.DoesNotContain(email, output, StringComparison.Ordinal);
        Assert.DoesNotContain(token, output, StringComparison.Ordinal);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
