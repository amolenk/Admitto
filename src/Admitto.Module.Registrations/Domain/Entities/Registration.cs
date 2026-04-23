using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

public class Registration : Aggregate<RegistrationId>
{
    private readonly List<TicketTypeSnapshot> _tickets = [];

    private Registration() { }

    private Registration(
        RegistrationId id,
        TeamId teamId,
        TicketedEventId eventId,
        EmailAddress email,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        AdditionalDetails additionalDetails,
        string eventName,
        string eventWebsiteUrl)
        : base(id)
    {
        TeamId = teamId;
        EventId = eventId;
        Email = email;
        _tickets = tickets.ToList();
        AdditionalDetails = additionalDetails;

        AddDomainEvent(new AttendeeRegisteredDomainEvent(
            teamId, eventId, id, email, "Attendee", eventName, eventWebsiteUrl));
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId EventId { get; private set; }
    public EmailAddress Email { get; private set; }
    public IReadOnlyList<TicketTypeSnapshot> Tickets => _tickets.AsReadOnly();
    public AdditionalDetails AdditionalDetails { get; private set; } = AdditionalDetails.Empty;

    public static Registration Create(
        TeamId teamId,
        TicketedEventId eventId,
        EmailAddress email,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        string eventName,
        string eventWebsiteUrl,
        AdditionalDetails? additionalDetails = null)
    {
        EnsureNoDuplicateSlugs(tickets);

        return new Registration(
            RegistrationId.New(),
            teamId,
            eventId,
            email,
            tickets,
            additionalDetails ?? AdditionalDetails.Empty,
            eventName,
            eventWebsiteUrl);
    }

    private static void EnsureNoDuplicateSlugs(IReadOnlyList<TicketTypeSnapshot> tickets)
    {
        var duplicates = tickets
            .GroupBy(t => t.Slug)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicates.Length > 0)
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypes(duplicates));
    }

    internal static class Errors
    {
        public static Error DuplicateTicketTypes(string[] slugs) =>
            new("duplicate_ticket_types", "One or more ticket types are duplicates.",
                Details: new Dictionary<string, object?> { ["slugs"] = slugs });
    }
}