namespace Scheduler.Api.DTOs;

public record DashboardSummaryResponse(
    int AppointmentsToday,
    int Clients,
    int Services,
    decimal ExpectedRevenue,
    string ExpectedRevenueFormatted,
    IEnumerable<AppointmentResponse> UpcomingAppointments,
    IEnumerable<ClientResponse> RecentClients,
    IEnumerable<ServiceResponse> TopServices
);
