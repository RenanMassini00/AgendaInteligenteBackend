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
    private readonly IWebHostEnvironment _environment;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailService> logger,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _logger = logger;
        _environment = environment;
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

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            client.CheckCertificateRevocation = false;

            if (_environment.IsDevelopment())
            {
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }

            var secureOption = _options.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            _logger.LogInformation(
                "Tentando enviar e-mail. Host: {Host}, Port: {Port}, UseSsl: {UseSsl}, Username: {Username}, ToEmail: {ToEmail}",
                _options.Host,
                _options.Port,
                _options.UseSsl,
                _options.Username,
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