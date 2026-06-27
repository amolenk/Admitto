using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetQRCode;

internal sealed class GetQRCodeFixture
{
    public static readonly TicketTypeId TicketTypeId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public const string PublicSlug = "devconf-2026";

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    private readonly bool _seedRegistration;
    private readonly bool _cancelRegistration;

    private GetQRCodeFixture(bool seedRegistration, bool cancelRegistration)
    {
        _seedRegistration = seedRegistration;
        _cancelRegistration = cancelRegistration;
    }

    public Guid RegistrationId { get; private set; }
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public static GetQRCodeFixture HappyFlow() => new(
        seedRegistration: true, cancelRegistration: false);

    public static GetQRCodeFixture WithCancelledRegistration() => new(
        seedRegistration: true, cancelRegistration: true);

    public static GetQRCodeFixture WithoutRegistration() => new(
        seedRegistration: false, cancelRegistration: false);

    public string QRCodeRoute(Guid registrationId, string publicSlug = PublicSlug) =>
        $"/e/{publicSlug}/qr-code/{registrationId}";

    public string OldPartnerQRCodeRoute(Guid registrationId, Guid? eventId = null) =>
        $"/api/events/{eventId ?? EventId}/registrations/{registrationId}/qr-code";

    public string PublicEventRoute(string publicSlug = PublicSlug) => $"/e/{publicSlug}";

    public string RegisterRoute(string publicSlug = PublicSlug) => $"/e/{publicSlug}/register";

    public string CancelRoute(Guid registrationId, string publicSlug = PublicSlug) =>
        $"/e/{publicSlug}/cancel/{registrationId}";

    public string EditRoute(Guid registrationId, string publicSlug = PublicSlug) =>
        $"/e/{publicSlug}/edit/{registrationId}";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();

        TeamId = team.Id.Value;

        var primaryEvent = BuildEvent(team.Id, "DevConf");
        EventId = primaryEvent.Id.Value;

        var primaryCatalog = TicketCatalog.Create(primaryEvent.Id, team.Id);
        primaryCatalog.AddTicketType(
            TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

        Registration? primaryRegistration = null;
        if (_seedRegistration)
        {
            primaryRegistration = Registration.Create(
                team.Id,
                primaryEvent.Id,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("General Admission"), [])]);

            RegistrationId = primaryRegistration.Id.Value;

            if (_cancelRegistration)
                primaryRegistration.Cancel(CancellationReason.AttendeeRequest);
        }

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(primaryEvent);
            db.TicketCatalogs.Add(primaryCatalog);
            if (primaryRegistration is not null)
                db.Registrations.Add(primaryRegistration);
        });
    }

    public static byte[] GenerateExpectedQRCode(Guid registrationId)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(registrationId.ToString(), QRCoder.QRCodeGenerator.ECCLevel.Q);

        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    private static TicketedEvent BuildEvent(TeamId teamId, string displayName)
    {
        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            TicketedEventId.New(),
            teamId,
            EventName.From(displayName),
            AbsoluteUrl.From("https://partner.example.com"),
            AbsoluteUrl.From("https://partner.example.com/tickets"),
            Slug.From(PublicSlug),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));
        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));
        return ticketedEvent;
    }
}
