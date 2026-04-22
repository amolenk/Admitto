using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee;

internal sealed class SelfRegisterAttendeeHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<SelfRegisterAttendeeCommand, RegistrationId>
{
    public async ValueTask<RegistrationId> HandleAsync(
        SelfRegisterAttendeeCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken);

        if (ticketedEvent is null)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        var policy = ticketedEvent.RegistrationPolicy;
        var now = timeProvider.GetUtcNow();

        if (policy is null)
            throw new BusinessRuleViolationException(Errors.RegistrationNotOpen);

        if (now < policy.OpensAt)
            throw new BusinessRuleViolationException(Errors.RegistrationNotOpen);

        if (now >= policy.ClosesAt)
            throw new BusinessRuleViolationException(Errors.RegistrationClosed);

        if (!policy.IsEmailDomainAllowed(command.Email.Value))
            throw new BusinessRuleViolationException(Errors.EmailDomainNotAllowed);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(Errors.NoTicketTypesConfigured);

        var ticketTypeMap = catalog.TicketTypes.ToDictionary(t => t.Id);
        ValidateTicketTypeSelection(command.TicketTypeSlugs, ticketTypeMap);

        var tickets = command.TicketTypeSlugs
            .Select(slug => new TicketTypeSnapshot(
                slug,
                ticketTypeMap[slug].TimeSlots.Select(ts => ts.Slug.Value).ToArray()))
            .ToList();

        try
        {
            catalog.Claim(command.TicketTypeSlugs, enforce: true);
        }
        catch (BusinessRuleViolationException ex)
            when (ex.Error.Code == TicketCatalog.Errors.EventNotActive.Code)
        {
            throw new BusinessRuleViolationException(Errors.EventNotActive);
        }

        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        var registration = Registration.Create(
            command.EventId,
            command.Email,
            tickets,
            additionalDetails);
        await writeStore.Registrations.AddAsync(registration, cancellationToken);

        return registration.Id;
    }

    internal static void ValidateTicketTypeSelection(
        string[] slugs,
        Dictionary<string, TicketType> ticketTypeMap)
    {
        var duplicates = slugs.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicates.Length > 0)
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypes(duplicates));

        var unknownSlugs = slugs.Where(s => !ticketTypeMap.ContainsKey(s)).ToArray();
        if (unknownSlugs.Length > 0)
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownSlugs));

        var cancelledSlugs = slugs.Where(s => ticketTypeMap[s].IsCancelled).ToArray();
        if (cancelledSlugs.Length > 0)
            throw new BusinessRuleViolationException(Errors.CancelledTicketTypes(cancelledSlugs));

        var allTimeSlots = slugs
            .SelectMany(s => ticketTypeMap[s].TimeSlots.Select(ts => ts.Slug.Value))
            .ToList();
        var overlapping = allTimeSlots.GroupBy(ts => ts).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (overlapping.Length > 0)
            throw new BusinessRuleViolationException(Errors.OverlappingTimeSlots(overlapping));
    }

    internal static class Errors
    {
        public static readonly Error EventNotFound = new(
            "registration.event_not_found",
            "The ticketed event could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error EventNotActive = new(
            "registration.event_not_active",
            "Cannot register for a cancelled or archived event.",
            Type: ErrorType.Validation);

        public static readonly Error RegistrationNotOpen = new(
            "registration.not_open",
            "Registration is not open for this event.",
            Type: ErrorType.Validation);

        public static readonly Error RegistrationClosed = new(
            "registration.closed",
            "Registration for this event has closed.",
            Type: ErrorType.Validation);

        public static readonly Error EmailDomainNotAllowed = new(
            "registration.email_domain_not_allowed",
            "Your email domain is not allowed for this event.",
            Type: ErrorType.Validation);

        public static readonly Error NoTicketTypesConfigured = new(
            "registration.no_ticket_types",
            "No ticket types have been configured for this event.",
            Type: ErrorType.Validation);

        public static Error DuplicateTicketTypes(string[] slugs) => new(
            "registration.duplicate_ticket_types",
            "Duplicate ticket types in selection.",
            Details: new Dictionary<string, object?> { ["slugs"] = slugs });

        public static Error UnknownTicketTypes(string[] slugs) => new(
            "registration.unknown_ticket_types",
            "One or more ticket types do not exist.",
            Details: new Dictionary<string, object?> { ["slugs"] = slugs });

        public static Error CancelledTicketTypes(string[] slugs) => new(
            "registration.cancelled_ticket_types",
            "One or more ticket types have been cancelled.",
            Details: new Dictionary<string, object?> { ["slugs"] = slugs });

        public static Error OverlappingTimeSlots(string[] slots) => new(
            "registration.overlapping_time_slots",
            "Selected ticket types have overlapping time slots.",
            Details: new Dictionary<string, object?> { ["slots"] = slots });
    }
}
