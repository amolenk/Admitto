using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.TestHelpers.TestData;
using Amolenk.Admitto.TestHelpers.TestFixtures;
using Azure.Messaging;

namespace Amolenk.Admitto.Worker.Tests;

[TestClass]
public abstract class WorkerTestsBase
{
    protected static AuthorizationTestFixture Authorization = AssemblyTestFixture.AuthorizationFixture;
    protected static DatabaseTestFixture Database = AssemblyTestFixture.DatabaseFixture;
    protected static EmailTestFixture Email = AssemblyTestFixture.EmailFixture;
    protected static IdentityTestFixture Identity = AssemblyTestFixture.IdentityFixture;
    protected static QueueStorageTestFixture QueueStorage = AssemblyTestFixture.QueueStorageFixture;

    protected Team DefaultTeam = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        await Task.WhenAll(
            Authorization.ResetAsync(),
            Database.ResetAsync(context =>
            {
                DefaultTeam = TeamDataFactory.CreateTeam(name: "Default Team",
                    emailSettings: Email.DefaultEmailSettings);
                
                context.Teams.Add(DefaultTeam);
            }),
            Identity.ResetAsync(),
            QueueStorage.ResetAsync());
    }

    protected static ValueTask PublishCommandAsync(ICommand command)
    {
        var message = OutboxMessage.FromCommand(command);

        return PublishMessageAsync(message);
    }

    protected static ValueTask PublishDomainEventAsync(IDomainEvent domainEvent)
    {
        var message = OutboxMessage.FromDomainEvent(domainEvent);

        return PublishMessageAsync(message);
    }

    private static async ValueTask PublishMessageAsync(OutboxMessage message)
    {
        var cloudEvent = new CloudEvent(nameof(Admitto), message.Type, new BinaryData(message.Data),
            "application/json")
        {
            Id = message.Id.ToString()
        };
        
        await QueueStorage.MessageQueue.SendMessageAsync(new BinaryData(cloudEvent));
    }
}