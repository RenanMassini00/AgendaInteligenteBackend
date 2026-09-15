using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string? toEmail, string subject, string htmlBody)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Email:Enabled está false.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Destinatário não informado. Subject: {Subject}", subject);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.Host) ||
            _options.Port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogError(
                "Configuração de SMTP incompleta. Defina Email:Host, Email:Port, Email:Username, Email:Password e Email:FromEmail em um armazenamento de segredos.");
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            client.Timeout = 30_000;
            client.CheckCertificateRevocation = true;

            var secureOption = _options.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            _logger.LogInformation(
                "Tentando enviar e-mail. Host: {Host}, Port: {Port}, UseSsl: {UseSsl}, ToEmail: {ToEmail}",
                _options.Host,
                _options.Port,
                _options.UseSsl,
                toEmail
            );

            await client.ConnectAsync(_options.Host, _options.Port, secureOption);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("E-mail enviado com sucesso para {ToEmail}.", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail para {ToEmail}.", toEmail);
            return false;
        }
    }
}
