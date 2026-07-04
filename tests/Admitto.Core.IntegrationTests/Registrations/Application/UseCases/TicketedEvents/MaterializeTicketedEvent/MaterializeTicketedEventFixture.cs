using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.MaterializeTicketedEvent;

internal sealed class MaterializeTicketedEventFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public Guid CreationRequestId { get; } = Guid.NewGuid();

    private MaterializeTicketedEventFixture() { }

    public static MaterializeTicketedEventFixture New() => new();

    public ValueTask SetupAsync(IntegrationTestEnvironment environment) => ValueTask.CompletedTask;
}
