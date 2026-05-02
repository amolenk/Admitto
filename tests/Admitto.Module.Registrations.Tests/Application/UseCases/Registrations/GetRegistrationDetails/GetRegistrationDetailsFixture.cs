using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using ActivityLogEntity = Amolenk.Admitto.Module.Registrations.Domain.Entities.ActivityLog;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.GetRegistrationDetails;

internal sealed class GetRegistrationDetailsFixture
{
    public const string TicketTypeSlug = "general-admission";
    public const string TicketTypeName = "General Admission";
    public const string VipSlug = "vip-pass";
    public const string VipName = "VIP Pass";

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketedEventId OtherEventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset ReconfirmedAt { get; private set; }

    private bool _withRegistration;
    private bool _withReconfirmed;
    private bool _withCancelled;
    private bool _withAdditionalDetails;
    private bool _withMultipleTickets;
    private bool _withRegisteredActivity;
    private bool _withReconfirmedActivity;
    private bool _withCancelledActivity;

    private GetRegistrationDetailsFixture() { }

    public static GetRegistrationDetailsFixture WithRegisteredAttendee() => new()
    {
        _withRegistration = true,
        _withRegisteredActivity = true,
    };

    public static GetRegistrationDetailsFixture WithReconfirmedAttendee() => new()
    {
        _withRegistration = true,
        _withReconfirmed = true,
        _withRegisteredActivity = true,
        _withReconfirmedActivity = true,
    };

    public static GetRegistrationDetailsFixture WithCancelledAttendee() => new()
    {
        _withRegistration = true,
        _withCancelled = true,
        _withRegisteredActivity = true,
        _withCancelledActivity = true,
    };

    public static GetRegistrationDetailsFixture WithAdditionalDetails() => new()
    {
        _withRegistration = true,
        _withAdditionalDetails = true,
        _withRegisteredActivity = true,
    };

    public static GetRegistrationDetailsFixture WithMultipleTickets() => new()
    {
        _withRegistration = true,
        _withMultipleTickets = true,
        _withRegisteredActivity = true,
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        RegisteredAt = DateTimeOffset.UtcNow.AddDays(-5);
        ReconfirmedAt = DateTimeOffset.UtcNow.AddDays(-2);

        if (!_withRegistration)
            return;

        var tickets = _withMultipleTickets
            ? new[]
            {
                new TicketTypeSnapshot(TicketTypeSlug, TicketTypeName, []),
                new TicketTypeSnapshot(VipSlug, VipName, []),
            }
            : new[] { new TicketTypeSnapshot(TicketTypeSlug, TicketTypeName, []) };

        AdditionalDetails? additionalDetails = null;
        if (_withAdditionalDetails)
            additionalDetails = AdditionalDetails.From(
                new Dictionary<string, string> { { "dietary", "vegan" } });

        var registration = Registration.Create(
            TeamId,
            EventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            tickets,
            additionalDetails);
        RegistrationId = registration.Id;

        if (_withReconfirmed)
            registration.Reconfirm(ReconfirmedAt);

        if (_withCancelled)
            registration.Cancel(CancellationReason.AttendeeRequest);

        await environment.Database.SeedAsync(db => db.Registrations.Add(registration));

        if (_withRegisteredActivity)
        {
            var registeredEntry = ActivityLogEntity.Create(
                registration.Id.Value,
                ActivityType.Registered,
                RegisteredAt);
            await environment.Database.SeedAsync(db => db.ActivityLog.Add(registeredEntry));
        }

        if (_withReconfirmedActivity)
        {
            var reconfirmedEntry = ActivityLogEntity.Create(
                registration.Id.Value,
                ActivityType.Reconfirmed,
                ReconfirmedAt);
            await environment.Database.SeedAsync(db => db.ActivityLog.Add(reconfirmedEntry));
        }

        if (_withCancelledActivity)
        {
            var cancelledEntry = ActivityLogEntity.Create(
                registration.Id.Value,
                ActivityType.Cancelled,
                DateTimeOffset.UtcNow.AddDays(-1),
                CancellationReason.AttendeeRequest.ToString());
            await environment.Database.SeedAsync(db => db.ActivityLog.Add(cancelledEntry));
        }
    }
}
