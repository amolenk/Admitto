using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Domain;

public sealed class BusinessRuleError
{
    public string Code { get; }
    public string MessageText { get; }

    private BusinessRuleError(string code, string messageText)
    {
        Code = code;
        MessageText = messageText;
    }

    public override string ToString() => $"{Code}: {MessageText}";

    public static class Attendee
    {
        public static BusinessRuleError NotFound(Guid id) =>
            new("attendee.not_found", $"Attendee with ID '{id}' does not exist.");

        public static BusinessRuleError StatusMismatch(AttendeeStatus expectedStatus, AttendeeStatus actualStatus) =>
            new("attendee.status_mismatch", $"Expected status '{expectedStatus.Humanize()}', but found '{actualStatus.Humanize()}'.");
    }

    public static class Entity
    {
        public static BusinessRuleError NotFound<TEntity>() =>
            new("entity.not_found", $"The specified {typeof(TEntity).Name.Humanize()} does not exist.");
    }
    
    public static class Team
    {
        public static readonly BusinessRuleError EmailIsRequired =
            new("team.email_is_required", "Team email address is required.");

        public static readonly BusinessRuleError MemberAlreadyExists =
            new("team.member_already_exists", "Team member already exists.");

        public static BusinessRuleError NotFound(string slug) =>
            new("team.not_found", $"Team with slug '{slug}' does not exist.");
    }
    
    public static class TicketedEvent
    {
        public static readonly BusinessRuleError EndTimeMustBeAfterStartTime =
            new("ticketed_event.end_time_must_be_after_start_time", "Event end time must be after start time.");

        public static readonly BusinessRuleError InsufficientCapacity =
            new("ticketed_event.insufficient_capacity", "Insufficient capacity for requested tickets.");

        public static readonly BusinessRuleError NameIsRequired =
            new("ticketed_event.name_is_required", "Event name is required.");

        public static BusinessRuleError NotFound(string slug) =>
            new("ticketed_event.not_found", $"Ticketed event with slug '{slug}' does not exist.");

        public static readonly BusinessRuleError RegistrationEndTimeMustBeAfterRegistrationStartTime =
            new("ticketed_event.registration_end_time_must_be_after_registration_start_time", "Registration start time must be before end time.");

        public static readonly BusinessRuleError RegistrationMustCloseBeforeEvent =
            new("ticketed_event.registration_must_close_before_event", "Registration must close before the event starts.");

        public static readonly BusinessRuleError TicketTypeAlreadyExists =
            new("ticketed_event.ticket_type_already_exists", "A ticket type with the same slug already exists.");

        public static readonly BusinessRuleError TicketsAreRequired =
            new("ticketed_event.tickets_are_required", "At least one ticket must be provided for reservation.");
    }
    
    public static class TicketType
    {
        public static BusinessRuleError MaxCapacityMustBeGreaterThan(int value) =>
            new("ticket_type.max_capacity_must_be_greater_than", $"Maximum capacity must be greater than {value}.");

        public static readonly BusinessRuleError NameIsRequired =
            new("ticket_type.name_is_required", "Ticket type name is required.");
    }
}