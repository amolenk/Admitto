using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RegisterExternalUser;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RegisterExternalUser.EventHandlers;
using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.RegisterExternalUser.EventHandlers;

[TestClass]
public sealed class UserCreatedDomainEventHandlerTests
{
    [TestMethod]
    public async ValueTask UserCreated_EnqueuesRegisterExternalUserCommand()
    {
        var userId = UserId.New();
        var domainEvent = new UserCreatedDomainEvent(userId, EmailAddress.From("user@example.com"));

        ICommand? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<ICommand>())).Do(ci => captured = ci.Arg<ICommand>());

        var handler = new UserCreatedDomainEventHandler(outbox);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        var command = captured.ShouldBeOfType<RegisterExternalUserCommand>();
        command.UserId.ShouldBe(userId.Value);
        command.CommandId.ShouldNotBe(Guid.Empty);

        var expectedCommandId = DeterministicCommandId<RegisterExternalUserCommand>.Create(domainEvent.EventId.Value);
        command.CommandId.ShouldBe(expectedCommandId);
    }
}
