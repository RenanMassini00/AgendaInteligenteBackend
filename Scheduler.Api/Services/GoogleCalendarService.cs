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

    public GoogleCalendarService(IOptions<GoogleCalendarOptions> options)
    {
        _options = options.Value;
    }

    public async Task<bool> CreateAppointmentEventAsync(
        User professional,
        UserSetting? userSetting,
        Client client,
        Service service,
        Appointment appointment)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        if (userSetting is null || string.IsNullOrWhiteSpace(userSetting.GoogleCalendarId))
        {
            return false;
        }

        if (!File.Exists(_options.ServiceAccountJsonPath))
        {
            return false;
        }

        var credential = GoogleCredential
            .FromFile(_options.ServiceAccountJsonPath)
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
            0
        );

        var endDateTime = new DateTime(
            appointment.AppointmentDate.Year,
            appointment.AppointmentDate.Month,
            appointment.AppointmentDate.Day,
            appointment.EndTime.Hours,
            appointment.EndTime.Minutes,
            0
        );

        var calendarEvent = new Event
        {
            Summary = $"{service.Name} - {client.FullName}",
            Description =
                $"Agendamento criado pelo sistema.\n\n" +
                $"Cliente: {client.FullName}\n" +
                $"Serviço: {service.Name}\n" +
                $"Telefone: {client.Phone}\n" +
                $"E-mail: {client.Email}",
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

        var request = calendarService.Events.Insert(calendarEvent, userSetting.GoogleCalendarId);
        request.SendUpdates = EventsResource.InsertRequest.SendUpdatesEnum.All;

        await request.ExecuteAsync();
        return true;
    }
}