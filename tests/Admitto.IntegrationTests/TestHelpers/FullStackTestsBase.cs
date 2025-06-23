using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;
using TeamDataFactory = Amolenk.Admitto.IntegrationTests.TestHelpers.Data.TeamDataFactory;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

[DoNotParallelize]
public abstract class FullStackTestsBase : ApiTestsBase
{
    protected Team DefaultTeam = null!;
    
    [TestInitialize]
    public override async Task TestInitialize()
    {
        await Task.WhenAll(
            Authorization.ResetAsync(),
            Database.ResetAsync(context =>
            {
                DefaultTeam = new TeamBuilder()
                    .WithEmailSettings(Email.DefaultEmailSettings)
                    .Build();

                context.Teams.Add(DefaultTeam);
            }),
            Email.ResetAsync(),
            Identity.ResetAsync(),
            QueueStorage.ResetAsync());

        await base.TestInitialize();
    }

    protected async ValueTask SeedDatabaseAsync(Action<ApplicationContext> seed)
    {
        seed(Database.Context);
    
        await Database.Context.SaveChangesAsync();

        // Reset the database context to ensure no stale data is present.
        Database.Context.ChangeTracker.Clear();
    }
    
    protected async ValueTask HandleCommand<TCommand, THandler>(TCommand command)
        where THandler : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        using var serviceScope = AssemblyTestFixture.WorkerHost.Services.CreateScope();

        var commandHandler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
        await commandHandler.HandleAsync(command, CancellationToken.None);
        
        var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync();
        
        // Reset the database context to ensure no stale data is present.
        Database.Context.ChangeTracker.Clear();
    }
    
    protected async Task HandleEvent<TEvent, THandler>(TEvent domainEvent)
        where THandler : IEventualDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        using var serviceScope = AssemblyTestFixture.WorkerHost.Services.CreateScope();

        var eventHandler = serviceScope.ServiceProvider.GetRequiredService<THandler>();
        await eventHandler.HandleAsync(domainEvent, CancellationToken.None);
        
        var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync();
        
        // Reset the database context to ensure no stale data is present.
        Database.Context.ChangeTracker.Clear();
    }
}