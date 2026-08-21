using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.AssignTeamMembership;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ChangeTeamMembershipRole;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Development;

internal sealed class LocalDemoSeedInitializer(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LocalDemoSeedInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("Development:LocalDemoSeed:Enabled"))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await SeedOrganizationAsync(stoppingToken))
                    return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Local demo organization seed will retry.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task<bool> SeedOrganizationAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<IOrganizationWriteStore>();
        var uow = services.GetRequiredKeyedService<IUnitOfWork>(OrganizationModule.Key);

        var aliceEmail = EmailAddress.From("alice@example.com");
        var demoTeamName = TeamName.From("Admitto Demo");
        var demoSlug = Slug.From("admitto-demo");
        var alice = await store.Users.FirstOrDefaultAsync(
            user => user.EmailAddress == aliceEmail, cancellationToken);
        if (alice is null || alice.ExternalUserId is null)
            return false;

        var candidates = await store.Teams
            .Where(candidate => candidate.Name == demoTeamName)
            .ToListAsync(cancellationToken);
        var memberTeams = candidates
            .Where(candidate => alice.Memberships.Any(item => item.TeamId == candidate.Id))
            .ToList();

        if (memberTeams.Count > 1 || (memberTeams.Count == 0 && candidates.Count > 1))
        {
            logger.LogError(
                "Local demo seed stopped: multiple ambiguous '{TeamName}' teams exist.", demoTeamName.Value);
            return true;
        }

        var team = memberTeams.SingleOrDefault();
        if (team is null && candidates.Count == 1)
        {
            logger.LogError(
                "Local demo seed stopped: existing '{TeamName}' team is not associated with Alice.",
                demoTeamName.Value);
            return true;
        }

        if (team is null)
        {
            var create = services.GetRequiredService<ICommandHandler<CreateTeamCommand, Guid>>();
            var teamId = await create.HandleAsync(new CreateTeamCommand(demoTeamName.Value), cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
            team = await store.Teams.GetAsync(
                candidate => candidate.Id == TeamId.From(teamId), cancellationToken);
        }

        var membership = alice.Memberships.FirstOrDefault(item => item.TeamId == team.Id);
        if (membership is null)
        {
            var assign = services.GetRequiredService<ICommandHandler<AssignTeamMembershipCommand>>();
            await assign.HandleAsync(new AssignTeamMembershipCommand(
                team.Id.Value, "alice@example.com", TeamMembershipRoleDto.Owner), cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
        }
        else if (membership.Role != TeamMembershipRole.Owner)
        {
            var change = services.GetRequiredService<ICommandHandler<ChangeTeamMembershipRoleCommand>>();
            await change.HandleAsync(new ChangeTeamMembershipRoleCommand(
                team.Id.Value, "alice@example.com", TeamMembershipRoleDto.Owner), cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
        }

        var request = team.EventCreationRequests.FirstOrDefault(item => item.PublicSlug == demoSlug);
        switch (LocalDemoSeedRequestState.Decide(request, demoSlug))
        {
            case LocalDemoSeedRequestDecision.AlreadyInFlight:
                return true;
            case LocalDemoSeedRequestDecision.AlreadyCreated:
                return true;
            case LocalDemoSeedRequestDecision.Terminal:
                logger.LogError(
                    "Local demo seed stopped: demo event creation request is in terminal state {Status}.",
                    request!.Status);
                return true;
        }

        var now = DateTimeOffset.UtcNow;
        var requestHandler = services.GetRequiredService<ICommandHandler<RequestTicketedEventCreationCommand, Guid>>();
        await requestHandler.HandleAsync(new RequestTicketedEventCreationCommand(
                team.Id.Value,
                alice.Id.Value,
                "Admitto Demo Event",
                "https://admitto-demo.local",
                "https://admitto-demo.local",
                now.AddDays(60),
                now.AddDays(61),
                "Europe/Amsterdam",
                demoSlug.Value), cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Requested local demo event creation for team {TeamId}.", team.Id.Value);

        return true;
    }
}
