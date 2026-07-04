using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.ExportRegistrations;

internal sealed class ExportRegistrationsFixture
{
    public static readonly TicketTypeId TicketTypeId =
        TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string Route => $"/admin/teams/{TeamId}/events/{EventId}/registrations/export";

    private readonly List<RegistrationSeed> _registrationSeeds = [];
    private readonly List<AdditionalDetailField> _schemaFields = [];

    private ExportRegistrationsFixture() { }

    public static ExportRegistrationsFixture HappyFlow() => new();

    public ExportRegistrationsFixture AddRegistration(
        string email,
        string firstName,
        string lastName,
        bool cancelled = false,
        IReadOnlyDictionary<string, string>? additionalDetails = null)
    {
        _registrationSeeds.Add(new RegistrationSeed(email, firstName, lastName, cancelled, additionalDetails));
        return this;
    }

    public ExportRegistrationsFixture WithAdditionalDetailField(string key, string name)
    {
        _schemaFields.Add(AdditionalDetailField.Create(key, name, AdditionalDetailField.MaxValueLength));
        return this;
    }

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        if (_schemaFields.Count > 0)
            ticketedEvent.UpdateAdditionalDetailSchema(_schemaFields);

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

        var registrations = _registrationSeeds.Select(seed =>
        {
            var snapshot = new TicketTypeSnapshot(
                TicketTypeId,
                TicketTypeName.From("General Admission"),
                []);
            var reg = Registration.Create(
                team.Id,
                eventId,
                EmailAddress.From(seed.Email),
                FirstName.From(seed.FirstName),
                LastName.From(seed.LastName),
                [snapshot],
                AdditionalDetails.From(seed.AdditionalDetails));
            if (seed.Cancelled)
                reg.Cancel(CancellationReason.AttendeeRequest);
            return reg;
        }).ToList();

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            foreach (var reg in registrations)
                db.Registrations.Add(reg);
        });
    }

    private sealed record RegistrationSeed(
        string Email,
        string FirstName,
        string LastName,
        bool Cancelled,
        IReadOnlyDictionary<string, string>? AdditionalDetails);
}
