using System.Security.Cryptography;
using System.Text;
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

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid OtherEventId { get; private set; }

    private readonly bool _seedRegistration;
    private readonly bool _cancelRegistration;
    private readonly bool _seedSecondEvent;

    private GetQRCodeFixture(bool seedRegistration, bool cancelRegistration, bool seedSecondEvent)
    {
        _seedRegistration = seedRegistration;
        _cancelRegistration = cancelRegistration;
        _seedSecondEvent = seedSecondEvent;
    }

    public Guid RegistrationId { get; private set; }
    public string SigningKeyBase64 { get; private set; } = "";
    public string OtherEventSigningKeyBase64 { get; private set; } = "";
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string ValidSignature => Sign(RegistrationId, SigningKeyBase64);

    public static GetQRCodeFixture HappyFlow() => new(
        seedRegistration: true, cancelRegistration: false, seedSecondEvent: false);

    public static GetQRCodeFixture WithCancelledRegistration() => new(
        seedRegistration: true, cancelRegistration: true, seedSecondEvent: false);

    public static GetQRCodeFixture WithoutRegistration() => new(
        seedRegistration: false, cancelRegistration: false, seedSecondEvent: false);

    public static GetQRCodeFixture WithSecondEvent() => new(
        seedRegistration: true, cancelRegistration: false, seedSecondEvent: true);

    public string Route(
        Guid registrationId,
        string? signature,
        Guid? teamId = null,
        Guid? eventId = null)
    {
        var path = $"/api/teams/{teamId ?? TeamId}/events/{eventId ?? EventId}/registrations/{registrationId}/qr-code";
        return signature is null ? path : $"{path}?signature={Uri.EscapeDataString(signature)}";
    }

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();

        TeamId = team.Id.Value;

        var primaryEvent = BuildEvent(team.Id, "DevConf");
        SigningKeyBase64 = primaryEvent.SigningKey;
        EventId = primaryEvent.Id.Value;

        TicketedEvent? otherEvent = null;
        if (_seedSecondEvent)
        {
            otherEvent = BuildEvent(team.Id, "OtherConf");
            OtherEventSigningKeyBase64 = otherEvent.SigningKey;
            OtherEventId = otherEvent.Id.Value;
        }

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

            if (otherEvent is not null)
            {
                db.TicketedEvents.Add(otherEvent);
                var otherCatalog = TicketCatalog.Create(otherEvent.Id, team.Id);
                otherCatalog.AddTicketType(
                    TicketTypeId, TicketTypeName.From("General Admission"), [], 100);
                db.TicketCatalogs.Add(otherCatalog);
            }
        });
    }

    public static string Sign(Guid registrationId, string keyBase64)
    {
        var key = Convert.FromBase64String(keyBase64);
        var payload = Encoding.ASCII.GetBytes(registrationId.ToString("N"));

        Span<byte> hash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(key, payload, hash);

        return Convert.ToBase64String(hash)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static byte[] GenerateExpectedQRCode(Guid registrationId, string signature)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(
            $"{registrationId}:{signature}",
            QRCoder.QRCodeGenerator.ECCLevel.Q);

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
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
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
