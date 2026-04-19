using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using CancellationPolicyEntity = Amolenk.Admitto.Module.Registrations.Domain.Entities.CancellationPolicy;
using ReconfirmPolicyEntity = Amolenk.Admitto.Module.Registrations.Domain.Entities.ReconfirmPolicy;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations;

internal sealed class PolicyEndpointFixture
{
    private const string TeamSlug = "policy-team";
    private const string EventSlug = "policy-event";

    private bool _cancelledEvent;
    private bool _seedCancellationPolicy;
    private DateTimeOffset _cancellationCutoff = DateTimeOffset.UtcNow.AddDays(7);
    private bool _seedReconfirmPolicy;
    private DateTimeOffset _reconfirmOpensAt = DateTimeOffset.UtcNow.AddDays(1);
    private DateTimeOffset _reconfirmClosesAt = DateTimeOffset.UtcNow.AddDays(5);
    private int _reconfirmCadenceDays = 2;

    private PolicyEndpointFixture() { }

    public static string CancellationPolicyRoute =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/cancellation-policy";

    public static string ReconfirmPolicyRoute =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/reconfirm-policy";

    public static PolicyEndpointFixture ActiveEvent() => new();

    public static PolicyEndpointFixture CancelledEvent() => new() { _cancelledEvent = true };

    public static PolicyEndpointFixture WithExistingCancellationPolicy() =>
        new() { _seedCancellationPolicy = true };

    public static PolicyEndpointFixture WithExistingReconfirmPolicy() =>
        new() { _seedReconfirmPolicy = true };

    public DateTimeOffset CancellationCutoff => _cancellationCutoff;

    public DateTimeOffset ReconfirmOpensAt => _reconfirmOpensAt;
    public DateTimeOffset ReconfirmClosesAt => _reconfirmClosesAt;
    public int ReconfirmCadenceDays => _reconfirmCadenceDays;

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        var ticketedEvent = new TicketedEventBuilder()
            .WithTeamId(team.Id)
            .WithSlug(EventSlug)
            .Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
            dbContext.TicketedEvents.Add(ticketedEvent);
        });

        var guard = TicketedEventLifecycleGuard.Create(TicketedEventId.From(ticketedEvent.Id.Value));
        if (_cancelledEvent)
        {
            guard.SetCancelled();
        }

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.TicketedEventLifecycleGuards.Add(guard);

            if (_seedCancellationPolicy)
            {
                var policy = CancellationPolicyEntity.Create(
                    TicketedEventId.From(ticketedEvent.Id.Value),
                    _cancellationCutoff);
                dbContext.CancellationPolicies.Add(policy);
            }

            if (_seedReconfirmPolicy)
            {
                var policy = ReconfirmPolicyEntity.Create(
                    TicketedEventId.From(ticketedEvent.Id.Value),
                    _reconfirmOpensAt,
                    _reconfirmClosesAt,
                    TimeSpan.FromDays(_reconfirmCadenceDays));
                dbContext.ReconfirmPolicies.Add(policy);
            }
        });
    }
}
