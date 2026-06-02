using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Contracts;

public interface IBookingAutomationService
{
    Task<(
        bool ClientEmailSent,
        bool ProfessionalEmailSent,
        bool ClientWhatsAppSent,
        bool ProfessionalWhatsAppSent,
        bool CalendarCreated
    )> ProcessAsync(
        User professional,
        UserSetting? userSetting,
        Client client,
        Service service,
        Appointment appointment
    );
}