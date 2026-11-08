using Gera.Application.Auth;
using Microsoft.Extensions.Logging;

namespace Gera.Infrastructure.Services;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Link de redefinição de senha para {Email}: {ResetLink}", email, resetLink);
        return Task.CompletedTask;
    }
}
