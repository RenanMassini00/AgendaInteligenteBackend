namespace Scheduler.Api.DTOs;

public record FinanceDailyItemResponse(
    string Date,
    decimal Amount,
    string AmountFormatted,
    int AppointmentsCount
);

public record FinanceAppointmentItemResponse(
    ulong Id,
    string ClientName,
    string ServiceName,
    string Date,
    string Time,
    string Status,
    decimal Amount,
    string AmountFormatted
);

public record FinanceStatusTotalResponse(
    string Status,
    string Label,
    int Count,
    decimal Amount,
    string AmountFormatted
);

public record FinanceServiceTotalResponse(
    string ServiceName,
    int Count,
    decimal Amount,
    string AmountFormatted
);

public record FinanceSummaryResponse(
    string Month,
    string MonthLabel,
    decimal ReceivedTotal,
    string ReceivedTotalFormatted,
    decimal ForecastTotal,
    string ForecastTotalFormatted,
    int AppointmentsCount,
    int CompletedAppointmentsCount,
    decimal AverageTicket,
    string AverageTicketFormatted,
    string? BestDayDate,
    decimal BestDayAmount,
    string BestDayAmountFormatted,
    decimal CompletionRate,
    string CompletionRateFormatted,
    string? TopServiceName,
    List<FinanceDailyItemResponse> DailyTotals,
    List<FinanceStatusTotalResponse> StatusTotals,
    List<FinanceServiceTotalResponse> ServiceTotals,
    List<FinanceAppointmentItemResponse> Appointments
);