using Amolenk.Admitto.Core.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent;

internal sealed class MaterializeTicketedEventFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public Guid CreationRequestId { get; } = Guid.NewGuid();

    private MaterializeTicketedEventFixture() { }

    public static MaterializeTicketedEventFixture New() => new();

    public ValueTask SetupAsync(IntegrationTestEnvironment environment) => ValueTask.CompletedTask;
}
