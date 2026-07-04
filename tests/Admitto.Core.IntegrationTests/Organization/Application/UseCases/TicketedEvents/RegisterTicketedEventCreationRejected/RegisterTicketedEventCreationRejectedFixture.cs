using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreationRejected;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreationRejected.EventHandlers;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreationRejected;

internal sealed class RegisterTicketedEventCreationRejectedFixture
{
    private bool _markAsProcessed;

    public Guid TeamId { get; private set; }
    public Guid CreationRequestId { get; private set; }
    public string Reason { get; } = "duplicate_slug";
    public TicketedEventCreationRejectedIntegrationEvent IntegrationEvent { get; private set; } = null!;

    private RegisterTicketedEventCreationRejectedFixture()
    {
    }

    public static RegisterTicketedEventCreationRejectedFixture PendingRequest() => new();

    public static RegisterTicketedEventCreationRejectedFixture AlreadyProcessed() => new() { _markAsProcessed = true };

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var team = new TeamBuilder().Build();
        var pendingRequest = team.RequestEventCreation(UserId.New(), DateTimeOffset.UtcNow);

        TeamId = team.Id.Value;
        CreationRequestId = pendingRequest.Id.Value;
        IntegrationEvent = new TicketedEventCreationRejectedIntegrationEvent(
            CreationRequestId,
            TeamId,
            Reason);

        await environment.OrganizationDatabase.SeedAsync(ctx =>
        {
            ctx.Teams.Add(team);

            if (_markAsProcessed)
            {
                ctx.ProcessedMessages.Add(ProcessedMessage.Create(MessageKey, DateTimeOffset.UtcNow));
            }
        }, cancellationToken);
    }

    public RegisterTicketedEventCreationRejectedCommand ToCommand() =>
        new(TeamId, CreationRequestId, Reason);

    public RegisterTicketedEventCreationRejectedHandler CreateHandler(IntegrationTestEnvironment environment) =>
        new(environment.OrganizationDatabase.Context);

    public TicketedEventCreationRejectedIntegrationEventHandler CreateIntegrationEventHandler(
        IntegrationTestEnvironment environment) =>
        new(CreateHandler(environment), new Inbox(environment.OrganizationDatabase.Context));

    private string MessageKey =>
        $"{IntegrationEvent.IntegrationEventId:N}.{typeof(TicketedEventCreationRejectedIntegrationEventHandler).FullName}";
}
