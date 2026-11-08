using Gera.Application.Auth;

namespace Gera.Infrastructure.Services;

public sealed class NullEmailSender : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
