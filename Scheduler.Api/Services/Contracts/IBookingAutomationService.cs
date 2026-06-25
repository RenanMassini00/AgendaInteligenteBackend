using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Contracts;

public interface IBookingAutomationService
{
    Task<(
        bool ClientEmailSent,
        bool ProfessionalEmailSent,
        bool ClientWhatsAppSent,
        bool ProfessionalWhatsAppSent,
        bool ClientPushSent,
        bool ProfessionalPushSent,
        bool CalendarCreated
    )> ProcessAsync(
        User professional,
        UserSetting? userSetting,
        Client client,
        Service service,
        Appointment appointment
    );
}
