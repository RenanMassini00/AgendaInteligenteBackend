using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using Scheduler.Api.Entities;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly GoogleCalendarOptions _options;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly IWebHostEnvironment _environment;

    public GoogleCalendarService(
        IOptions<GoogleCalendarOptions> options,
        ILogger<GoogleCalendarService> logger,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task<bool> CreateAppointmentEventAsync(
        User professional,
        UserSetting? userSetting,
        Client client,
        Service service,
        Appointment appointment)
    {
        try
        {
            if (!_options.Enabled)
            {
                _logger.LogWarning("Google Calendar desabilitado em configuração.");
                return false;
            }

            var calendarId = !string.IsNullOrWhiteSpace(userSetting?.GoogleCalendarId)
                ? userSetting!.GoogleCalendarId!.Trim()
                : professional.Email?.Trim();

            if (string.IsNullOrWhiteSpace(calendarId))
            {
                _logger.LogWarning(
                    "CalendarId não configurado para o usuário {UserId}.",
                    professional.Id
                );
                return false;
            }

            var jsonPath = Path.Combine(_environment.ContentRootPath, _options.ServiceAccountJsonPath);

            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("Arquivo da service account não encontrado em: {Path}", jsonPath);
                return false;
            }

            var credential = GoogleCredential
                .FromFile(jsonPath)
                .CreateScoped(CalendarService.Scope.Calendar);

            var calendarService = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _options.ApplicationName
            });

            var startDateTime = new DateTime(
                appointment.AppointmentDate.Year,
                appointment.AppointmentDate.Month,
                appointment.AppointmentDate.Day,
                appointment.StartTime.Hours,
                appointment.StartTime.Minutes,
                0,
                DateTimeKind.Unspecified
            );

            var endDateTime = new DateTime(
                appointment.AppointmentDate.Year,
                appointment.AppointmentDate.Month,
                appointment.AppointmentDate.Day,
                appointment.EndTime.Hours,
                appointment.EndTime.Minutes,
                0,
                DateTimeKind.Unspecified
            );

            var calendarEvent = new Event
            {
                Summary = $"{service.Name} - {client.FullName}",
                Description =
                    $"Agendamento criado pelo sistema.\n\n" +
                    $"Cliente: {client.FullName}\n" +
                    $"Serviço: {service.Name}\n" +
                    $"Telefone: {client.Phone}\n" +
                    $"E-mail: {client.Email ?? "Não informado"}\n" +
                    $"Observações: {appointment.Notes ?? "Sem observações"}",
                Start = new EventDateTime
                {
                    DateTime = startDateTime,
                    TimeZone = _options.DefaultTimezone
                },
                End = new EventDateTime
                {
                    DateTime = endDateTime,
                    TimeZone = _options.DefaultTimezone
                },
                Attendees = string.IsNullOrWhiteSpace(client.Email)
                    ? null
                    : new List<EventAttendee>
                    {
                        new() { Email = client.Email.Trim() }
                    }
            };

            var request = calendarService.Events.Insert(calendarEvent, calendarId);
            request.SendUpdates = EventsResource.InsertRequest.SendUpdatesEnum.All;

            var created = await request.ExecuteAsync();

            _logger.LogInformation(
                "Evento criado com sucesso no Google Calendar. EventId: {EventId}, CalendarId: {CalendarId}",
                created.Id,
                calendarId
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao criar evento na agenda do usuário {UserId}.",
                professional.Id
            );
            return false;
        }
    }
}