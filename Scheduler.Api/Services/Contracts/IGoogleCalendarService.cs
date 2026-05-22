using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Contracts;

public interface IGoogleCalendarService
{
    Task<bool> CreateAppointmentEventAsync(
        User professional,
        UserSetting? userSetting,
        Client client,
        Service service,
        Appointment appointment
    );
}