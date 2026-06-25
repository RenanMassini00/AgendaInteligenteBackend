using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;
using WebPush;
using BrowserPushSubscription = WebPush.PushSubscription;

namespace Scheduler.Api.Services;

public class WebPushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _context;
    private readonly WebPushOptions _options;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        AppDbContext context,
        IOptions<WebPushOptions> options,
        ILogger<WebPushNotificationService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> RegisterSubscriptionAsync(
        ulong userId,
        PushSubscriptionRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint) ||
            string.IsNullOrWhiteSpace(request.Keys?.P256dh) ||
            string.IsNullOrWhiteSpace(request.Keys.Auth))
        {
            return false;
        }

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);

        if (!userExists)
        {
            return false;
        }

        var endpoint = request.Endpoint.Trim();
        var endpointHash = ComputeEndpointHash(endpoint);

        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(x => x.EndpointHash == endpointHash, cancellationToken);

        if (subscription is null)
        {
            subscription = new WebPushSubscription
            {
                UserId = userId,
                EndpointHash = endpointHash,
                Endpoint = endpoint,
                CreatedAt = DateTime.Now
            };

            _context.PushSubscriptions.Add(subscription);
        }

        subscription.UserId = userId;
        subscription.Endpoint = endpoint;
        subscription.P256dh = request.Keys.P256dh.Trim();
        subscription.Auth = request.Keys.Auth.Trim();
        subscription.ExpirationTime = request.ExpirationTime;
        subscription.UserAgent = string.IsNullOrWhiteSpace(request.UserAgent)
            ? null
            : request.UserAgent.Trim();
        subscription.DeviceName = string.IsNullOrWhiteSpace(request.DeviceName)
            ? null
            : request.DeviceName.Trim();
        subscription.IsActive = true;
        subscription.FailureCount = 0;
        subscription.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveSubscriptionAsync(
        ulong userId,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return false;
        }

        var endpointHash = ComputeEndpointHash(endpoint.Trim());
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.EndpointHash == endpointHash,
                cancellationToken);

        if (subscription is null)
        {
            return false;
        }

        subscription.IsActive = false;
        subscription.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SendToUserAsync(
        ulong userId,
        string title,
        string body,
        string? url = null,
        string? tag = null,
        ulong? appointmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("Web Push desabilitado ou sem chaves VAPID configuradas.");
            return false;
        }

        var subscriptions = await _context.PushSubscriptions
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            return false;
        }

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            icon = _options.IconUrl,
            badge = _options.BadgeUrl,
            url = string.IsNullOrWhiteSpace(url) ? _options.DefaultUrl : url,
            tag,
            appointmentId
        });

        var sent = 0;
        var vapidDetails = new VapidDetails(
            _options.Subject,
            _options.PublicKey,
            _options.PrivateKey);

        using var client = new WebPushClient();

        foreach (var savedSubscription in subscriptions)
        {
            var pushSubscription = new BrowserPushSubscription(
                savedSubscription.Endpoint,
                savedSubscription.P256dh,
                savedSubscription.Auth);

            try
            {
                await client.SendNotificationAsync(
                    pushSubscription,
                    payload,
                    vapidDetails,
                    cancellationToken);

                savedSubscription.LastSuccessAt = DateTime.Now;
                savedSubscription.FailureCount = 0;
                savedSubscription.UpdatedAt = DateTime.Now;
                sent++;
            }
            catch (WebPushException ex) when (
                ex.StatusCode == HttpStatusCode.NotFound ||
                ex.StatusCode == HttpStatusCode.Gone)
            {
                savedSubscription.IsActive = false;
                savedSubscription.LastFailureAt = DateTime.Now;
                savedSubscription.FailureCount++;
                savedSubscription.UpdatedAt = DateTime.Now;

                _logger.LogWarning(
                    "Assinatura push expirada para usuário {UserId}. Status: {StatusCode}",
                    userId,
                    ex.StatusCode);
            }
            catch (Exception ex)
            {
                savedSubscription.LastFailureAt = DateTime.Now;
                savedSubscription.FailureCount++;
                savedSubscription.UpdatedAt = DateTime.Now;

                _logger.LogError(
                    ex,
                    "Erro ao enviar push para usuário {UserId}.",
                    userId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return sent > 0;
    }

    public async Task<(bool ClientPushSent, bool ProfessionalPushSent)> SendAppointmentCreatedAsync(
        User professional,
        Client client,
        Service service,
        Appointment appointment,
        CancellationToken cancellationToken = default)
    {
        var dateText = appointment.AppointmentDate.ToString("dd/MM/yyyy");
        var timeText = $"{appointment.StartTime:hh\\:mm} às {appointment.EndTime:hh\\:mm}";
        var businessName = string.IsNullOrWhiteSpace(professional.BusinessName)
            ? professional.FullName
            : professional.BusinessName;

        var professionalPushSent = await SendToUserAsync(
            professional.Id,
            "Novo agendamento recebido",
            $"{client.FullName} agendou {service.Name} em {dateText}, {timeText}.",
            _options.ProfessionalUrl,
            $"appointment-{appointment.Id}",
            appointment.Id,
            cancellationToken);

        var clientUserId = await _context.Users
            .AsNoTracking()
            .Where(x =>
                x.Role == "client" &&
                x.IsActive &&
                x.ClientId == client.Id &&
                x.ProfessionalUserId == professional.Id)
            .Select(x => (ulong?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var clientPushSent = false;

        if (clientUserId is not null)
        {
            clientPushSent = await SendToUserAsync(
                clientUserId.Value,
                "Agendamento confirmado",
                $"{service.Name} com {businessName} em {dateText}, {timeText}.",
                _options.ClientUrl,
                $"appointment-{appointment.Id}",
                appointment.Id,
                cancellationToken);
        }

        return (clientPushSent, professionalPushSent);
    }

    private bool IsConfigured()
    {
        return _options.Enabled &&
               !string.IsNullOrWhiteSpace(_options.Subject) &&
               !string.IsNullOrWhiteSpace(_options.PublicKey) &&
               !string.IsNullOrWhiteSpace(_options.PrivateKey);
    }

    private static string ComputeEndpointHash(string endpoint)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(endpoint));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
