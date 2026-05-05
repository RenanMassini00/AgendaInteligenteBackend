using Scheduler.Api.Entities;

namespace Scheduler.Api.Utils;

public static class SlotCalculator
{
    public static List<(TimeSpan Start, TimeSpan End)> BuildAvailableSlots(
        DateTime targetDate,
        int serviceDurationMinutes,
        IEnumerable<WeeklyAvailability> availabilities,
        IEnumerable<Appointment> appointments,
        IEnumerable<BlockedPeriod> blockedPeriods)
    {
        var slots = new List<(TimeSpan Start, TimeSpan End)>();
        if (serviceDurationMinutes <= 0) return slots;

        var weekday = (int)targetDate.DayOfWeek;
        var dayAvailabilities = availabilities
            .Where(x => x.IsActive && x.Weekday == weekday)
            .OrderBy(x => x.StartTime)
            .ToList();

        if (dayAvailabilities.Count == 0) return slots;

        var step = TimeSpan.FromMinutes(serviceDurationMinutes);
        var dayAppointments = appointments
            .Where(x => x.AppointmentDate.Date == targetDate.Date && x.Status != "cancelled")
            .OrderBy(x => x.StartTime)
            .ToList();

        var dayBlocked = blockedPeriods
            .Where(x => x.StartDatetime.Date <= targetDate.Date && x.EndDatetime.Date >= targetDate.Date)
            .OrderBy(x => x.StartDatetime)
            .ToList();

        foreach (var availability in dayAvailabilities)
        {
            var cursor = availability.StartTime;
            while (cursor.Add(step) <= availability.EndTime)
            {
                var candidateEnd = cursor.Add(step);
                var conflictsAppointment = dayAppointments.Any(a => cursor < a.EndTime && candidateEnd > a.StartTime);
                var conflictsBlocked = dayBlocked.Any(block =>
                {
                    var blockStart = block.StartDatetime.Date == targetDate.Date ? block.StartDatetime.TimeOfDay : TimeSpan.Zero;
                    var blockEnd = block.EndDatetime.Date == targetDate.Date ? block.EndDatetime.TimeOfDay : new TimeSpan(23, 59, 59);
                    return cursor < blockEnd && candidateEnd > blockStart;
                });

                if (!conflictsAppointment && !conflictsBlocked)
                {
                    slots.Add((cursor, candidateEnd));
                }

                cursor = cursor.Add(TimeSpan.FromMinutes(15));
            }
        }

        return slots;
    }
}
