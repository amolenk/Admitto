namespace Amolenk.Admitto.Domain.ValueObjects;

public record ReminderPolicy(TimeSpan LeadTime)
{
    public static ReminderPolicy Default => new(TimeSpan.FromDays(3));
}