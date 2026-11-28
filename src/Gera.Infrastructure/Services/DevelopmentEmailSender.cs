using Gera.Application.Auth;
using Microsoft.Extensions.Logging;

namespace Gera.Infrastructure.Services;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Redefinição solicitada, mas o e-mail SMTP não está configurado. Nenhum link foi enviado.");
        return Task.CompletedTask;
    }
}
