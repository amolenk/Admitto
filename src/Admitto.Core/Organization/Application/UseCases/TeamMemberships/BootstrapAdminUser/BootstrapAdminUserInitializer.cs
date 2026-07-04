using Amolenk.Admitto.Core.Organization.Application.ExternalUsers;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;

/// <summary>
/// Hosted service that ensures a bootstrap administrator user exists on startup.
/// Idempotent: safe to run on every restart and under concurrent startup scenarios.
/// </summary>
public sealed class BootstrapAdminUserInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<BootstrapAdminUserOptions> options,
    ILogger<BootstrapAdminUserInitializer> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = options.Value.EmailAddress;
        if (string.IsNullOrWhiteSpace(email))
            return;

        logger.LogInformation("Bootstrapping admin user with email {Email}.", email);

        await using var scope = scopeFactory.CreateAsyncScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IOrganizationWriteStore>();
        var userDirectory = scope.ServiceProvider.GetRequiredService<IExternalUserDirectory>();
        var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(OrganizationModule.Key);

        var emailAddress = EmailAddress.From(email);

        var user = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (user is null)
        {
            user = User.CreateAdmin(emailAddress);
            writeStore.Users.Add(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created bootstrap admin user {UserId}.", user.Id.Value);
        }

        if (user.ExternalUserId is null)
        {
            logger.LogInformation("Provisioning bootstrap admin user {UserId} in Keycloak.", user.Id.Value);
            var externalUserId = await userDirectory.InviteUserAsync(email, cancellationToken);

            user.AssignExternalUserId(ExternalUserId.From(externalUserId));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Bootstrap admin user {UserId} provisioned in Keycloak.", user.Id.Value);
        }
        else
        {
            logger.LogInformation("Bootstrap admin user {UserId} already provisioned, skipping.", user.Id.Value);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
