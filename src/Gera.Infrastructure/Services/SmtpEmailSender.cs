using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;

namespace Gera.Infrastructure.Services;

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Gera";

    public bool IsValid() => !string.IsNullOrWhiteSpace(Host) && Port is > 0 and <= 65535 && EnableSsl &&
        MailAddress.TryCreate(FromAddress, out _) && !string.IsNullOrWhiteSpace(FromName) &&
        ((string.IsNullOrWhiteSpace(UserName) && string.IsNullOrWhiteSpace(Password)) ||
         (!string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password)));
}

public sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options) : Gera.Application.Auth.IEmailSender
{
    public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        using var message = new MailMessage
        {
            From = new MailAddress(config.FromAddress, config.FromName),
            Subject = "Redefinição de senha do Gera",
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(email));
        var safeLink = HtmlEncoder.Default.Encode(resetLink);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            $"<p>Recebemos uma solicitação para redefinir sua senha do Gera.</p><p><a href=\"{safeLink}\">Redefinir senha</a></p><p>Se você não fez essa solicitação, ignore esta mensagem.</p>", null, "text/html"));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            $"Recebemos uma solicitação para redefinir sua senha do Gera. Abra este link: {resetLink}\n\nSe você não fez essa solicitação, ignore esta mensagem.", null, "text/plain"));

        using var smtp = new SmtpClient(config.Host, config.Port)
        {
            EnableSsl = config.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(config.UserName))
            smtp.Credentials = new NetworkCredential(config.UserName, config.Password);
        await smtp.SendMailAsync(message, cancellationToken);
    }
}
