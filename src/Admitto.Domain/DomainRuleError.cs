using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Domain;

public sealed class DomainRuleError
{
    public string Code { get; }
    public string MessageText { get; }

    private DomainRuleError(string code, string messageText)
    {
        Code = code;
        MessageText = messageText;
    }

    public override string ToString() => $"{Code}: {MessageText}";

    public static class Attendee
    {
        public static DomainRuleError CannotReconfirmInStatus(RegistrationStatus status) =>
            new("attendee.cannot_reconfirm", $"Cannot reconfirm attendance when registration is in status '{status}'.");
        
        public static DomainRuleError UnexpectedAdditionalDetail(string name) =>
            new("attendee.unexpected_detail", $"Attendee detail '{name}' is unexpected.");

        public static DomainRuleError MissingAdditionalDetail(string name) =>
            new("attendee.missing_detail", $"Attendee detail '{name}' is missing.");

        public static DomainRuleError InvalidAdditionalDetail(string name) =>
            new("attendee.invalid_detail", $"Attendee detail '{name}' is invalid.");

    }
    
    public static class Contributor
    {
        public static DomainRuleError UnsupportedRole(string role) =>
            new("contributor.unsupported_role", $"The specified contributor role '{role}' is not supported.");
    }
    
    public static class Email
    {
        public static DomainRuleError UnsupportedType =>
            new("email.unsupported_type", "The specified email type is not supported.");
    }

    public static class Entity
    {
        public static DomainRuleError NotFound<TEntity>() =>
            new("entity.not_found", $"The specified {typeof(TEntity).Name.Humanize()} does not exist.");
    }

    public static class Registration
    {
        // public static DomainRuleError NotFound(Guid id) =>
        //     new("attendee.not_found", $"Attendee with ID '{id}' does not exist.");
        //
        // public static DomainRuleError StatusMismatch(AttendeeStatus expectedStatus, AttendeeStatus actualStatus) =>
        //     new("attendee.status_mismatch", $"Expected status '{expectedStatus.Humanize()}', but found '{actualStatus.Humanize()}'.");

        public static DomainRuleError AlreadyCanceled =>
            new("registration.already_canceled", "Registration is already canceled.");

        public static DomainRuleError CannotCancelAfterEventStart => new(
            "registration.cancellation_period_over",
            "Cannot cancel registration after the event has started.");
        
        public static DomainRuleError CannotCancelInStatus(RegistrationStatus status) => new(
            "registration.invalid_status_for_cancellation",
            $"Cannot cancel registration with status '{status}'.");
    }

    public static class Team
    {
        public static readonly DomainRuleError EmailIsRequired =
            new("team.email_is_required", "Team email address is required.");

        public static readonly DomainRuleError MemberAlreadyExists =
            new("team.member_already_exists", "Team member already exists.");

        public static DomainRuleError NotFound(string slug) =>
            new("team.not_found", $"Team with slug '{slug}' does not exist.");

        public static DomainRuleError NotFound(Guid teamId) =>
            new("team.not_found", $"Team with ID '{teamId}' does not exist.");
    }

    public static class TicketedEvent
    {
        public static DomainRuleError EndTimeMustBeAfterStartTime =>
            new("ticketed_event.end_time_must_be_after_start_time", "Event end time must be after start time.");

        public static DomainRuleError InsufficientCapacity =>
            new("ticketed_event.insufficient_capacity", "Insufficient capacity for requested tickets.");

        public static DomainRuleError NameIsRequired =>
            new("ticketed_event.name_is_required", "Event name is required.");

        public static DomainRuleError RegistrationClosed =>
            new DomainRuleError("ticketed_event.registration_closed", "Registration for this event is currently closed.");

        public static DomainRuleError NotFound(string slug) =>
            new("ticketed_event.not_found", $"Ticketed event with slug '{slug}' does not exist.");

        public static DomainRuleError NotFound(Guid eventId) =>
            new("ticketed_event.not_found", $"Ticketed event with ID '{eventId}' does not exist.");

        public static DomainRuleError RegistrationEndTimeMustBeAfterRegistrationStartTime =>
            new(
                "ticketed_event.registration_end_time_must_be_after_registration_start_time",
                "Registration start time must be before end time.");

        public static DomainRuleError RegistrationMustCloseBeforeEvent =>
            new(
                "ticketed_event.registration_must_close_before_event",
                "Registration must close before the event starts.");

        public static DomainRuleError TicketTypeAlreadyExists =>
            new("ticketed_event.ticket_type_already_exists", "A ticket type with the same slug already exists.");

        public static DomainRuleError TicketsAreRequired =>
            new("ticketed_event.tickets_are_required", "At least one ticket must be provided for reservation.");

        public static DomainRuleError CapacityExceeded(string slug) =>
            new("ticketed_event.capacity_exceeded", $"The capacity for ticket type '{slug}' has been exceeded.");

        public static DomainRuleError InvalidTicketType(string slug) =>
            new("ticketed_event.invalid_ticket_type", $"The ticket type '{slug}' is invalid.");

        public static DomainRuleError OverlappingSlots() =>
            new("ticketed_event.overlapping_slots", "Cannot register for tickets that have overlapping time slots. Please select tickets for different time periods.");
    }

    public static class TicketType
    {
        public static DomainRuleError NameIsRequired =>
            new("ticket_type.name_is_required", "Ticket type name is required.");

        public static DomainRuleError SlotNamesAreRequired =>
            new("ticket_type.slot_names_are_required", "Ticket type must have at least one slot.");
    }
}