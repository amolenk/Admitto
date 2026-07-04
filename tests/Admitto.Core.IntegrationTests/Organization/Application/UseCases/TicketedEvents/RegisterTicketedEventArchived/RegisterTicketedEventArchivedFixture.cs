using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived.EventHandlers;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;

internal sealed class RegisterTicketedEventArchivedFixture
{
    private bool _markAsProcessed;

    public Guid TeamId { get; private set; }
    public TicketedEventId TicketedEventId { get; private set; }
    public TicketedEventArchivedIntegrationEvent IntegrationEvent { get; private set; } = null!;

    private RegisterTicketedEventArchivedFixture()
    {
    }

    public static RegisterTicketedEventArchivedFixture ActiveEvent() => new();

    public static RegisterTicketedEventArchivedFixture AlreadyProcessed() => new() { _markAsProcessed = true };

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var team = new TeamBuilder().Build();
        var pendingRequest = team.RequestEventCreation(UserId.New(), DateTimeOffset.UtcNow);

        TicketedEventId = TicketedEventId.New();
        team.RegisterEventCreated(pendingRequest.Id, TicketedEventId, DateTimeOffset.UtcNow);

        TeamId = team.Id.Value;
        IntegrationEvent = new TicketedEventArchivedIntegrationEvent(TeamId, TicketedEventId.Value);

        await environment.OrganizationDatabase.SeedAsync(ctx =>
        {
            ctx.Teams.Add(team);

            if (_markAsProcessed)
            {
                ctx.ProcessedMessages.Add(ProcessedMessage.Create(MessageKey, DateTimeOffset.UtcNow));
            }
        }, cancellationToken);
    }

    public RegisterTicketedEventArchivedCommand ToCommand() =>
        new(TeamId, TicketedEventId.Value);

    public RegisterTicketedEventArchivedHandler CreateHandler(IntegrationTestEnvironment environment) =>
        new(environment.OrganizationDatabase.Context);

    public TicketedEventArchivedIntegrationEventHandler CreateIntegrationEventHandler(
        IntegrationTestEnvironment environment) =>
        new(CreateHandler(environment), new Inbox(environment.OrganizationDatabase.Context));

    private string MessageKey =>
        $"{IntegrationEvent.IntegrationEventId:N}.{typeof(TicketedEventArchivedIntegrationEventHandler).FullName}";
}
