using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreated;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreated.EventHandlers;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreated;

internal sealed class RegisterTicketedEventCreatedFixture
{
    private const string TimeZone = "Europe/Amsterdam";

    private bool _markAsProcessed;

    public Guid TeamId { get; private set; }
    public Guid CreationRequestId { get; private set; }
    public Guid TicketedEventId { get; private set; } = Guid.NewGuid();
    public TicketedEventCreatedIntegrationEvent IntegrationEvent { get; private set; } = null!;

    private RegisterTicketedEventCreatedFixture()
    {
    }

    public static RegisterTicketedEventCreatedFixture PendingRequest() => new();

    public static RegisterTicketedEventCreatedFixture AlreadyProcessed() => new() { _markAsProcessed = true };

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var team = new TeamBuilder().Build();
        var pendingRequest = team.RequestEventCreation(UserId.New(), DateTimeOffset.UtcNow);

        TeamId = team.Id.Value;
        CreationRequestId = pendingRequest.Id.Value;
        IntegrationEvent = new TicketedEventCreatedIntegrationEvent(
            CreationRequestId,
            TeamId,
            TicketedEventId,
            TimeZone);

        await environment.OrganizationDatabase.SeedAsync(ctx =>
        {
            ctx.Teams.Add(team);

            if (_markAsProcessed)
            {
                ctx.ProcessedMessages.Add(ProcessedMessage.Create(MessageKey, DateTimeOffset.UtcNow));
            }
        }, cancellationToken);
    }

    public RegisterTicketedEventCreatedCommand ToCommand() =>
        new(TeamId, CreationRequestId, TicketedEventId);

    public RegisterTicketedEventCreatedHandler CreateHandler(IntegrationTestEnvironment environment) =>
        new(environment.OrganizationDatabase.Context);

    public TicketedEventCreatedIntegrationEventHandler CreateIntegrationEventHandler(
        IntegrationTestEnvironment environment) =>
        new(CreateHandler(environment), new Inbox(environment.OrganizationDatabase.Context));

    public UnitOfWork<OrganizationDbContext> CreateUnitOfWork(IntegrationTestEnvironment environment) =>
        new(
            environment.OrganizationDatabase.Context,
            new NoOpOutboxMessageSender(),
            NullLogger<UnitOfWork<OrganizationDbContext>>.Instance);

    public async ValueTask MarkAsConcurrentlyProcessedAsync(
        IntegrationTestEnvironment environment,
        CancellationToken cancellationToken)
    {
        await environment.OrganizationDatabase.Context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             insert into organization.processed_messages (id, message_key, processed_at)
             values ({Guid.NewGuid()}, {MessageKey}, {DateTimeOffset.UtcNow})
             """,
            cancellationToken);
    }

    private string MessageKey =>
        $"{IntegrationEvent.IntegrationEventId:N}.{typeof(TicketedEventCreatedIntegrationEventHandler).FullName}";

    private sealed class NoOpOutboxMessageSender : IOutboxMessageSender
    {
        public ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
