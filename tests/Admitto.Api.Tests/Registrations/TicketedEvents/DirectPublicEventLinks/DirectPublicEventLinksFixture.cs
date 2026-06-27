using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Registrations.TicketedEvents.DirectPublicEventLinks;

internal sealed class DirectPublicEventLinksFixture
{
    private const string PublicSlug = "devconf-2026";

    public Guid RegistrationId { get; } = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static DirectPublicEventLinksFixture HappyFlow() => new();

    public string PublicEventRoute(string publicSlug = PublicSlug) => $"/e/{publicSlug}";

    public string RegisterRoute(string publicSlug = PublicSlug) => $"/e/{publicSlug}/register";

    public string CancelRoute(Guid registrationId, string publicSlug = PublicSlug) =>
        $"/e/{publicSlug}/cancel/{registrationId}";

    public string EditRoute(Guid registrationId, string publicSlug = PublicSlug) =>
        $"/e/{publicSlug}/edit/{registrationId}";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            TicketedEventId.New(),
            TeamId.From(Guid.NewGuid()),
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://partner.example.com"),
            AbsoluteUrl.From("https://partner.example.com/tickets"),
            Slug.From(PublicSlug),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
        });
    }
}
