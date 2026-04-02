using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Application.Services;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amolenk.Admitto.Module.Organization.Application.Jobs;

/// <summary>
/// Scheduled job that executes IdP deprovisioning for users whose grace period has expired.
/// Implements US-006 (SC-013): runs on a recurring schedule, finds users with
/// <c>DeprovisionAfter &lt;= now</c>, removes their IdP account, and clears the user's
/// <c>ExternalUserId</c> and <c>DeprovisionAfter</c> fields.
/// </summary>
[RequiresCapability(HostCapability.Jobs)]
[DisallowConcurrentExecution]
public sealed class DeprovisionUserIdpJob(
    IOrganizationWriteStore writeStore,
    IExternalUserDirectory userDirectory,
    [FromKeyedServices(OrganizationModuleKey.Value)] IUnitOfWork unitOfWork,
    ILogger<DeprovisionUserIdpJob> logger)
    : IJob
{
    public const string Name = nameof(DeprovisionUserIdpJob);

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;

            var users = await writeStore.Users
                .Where(u => u.DeprovisionAfter != null && u.DeprovisionAfter <= now)
                .ToListAsync(context.CancellationToken);

            foreach (var user in users)
            {
                logger.LogInformation(
                    "Deprovisioning IdP account for user {UserId} (DeprovisionAfter: {DeprovisionAfter})",
                    user.Id.Value, user.DeprovisionAfter);

                if (user.ExternalUserId is not null)
                {
                    await userDirectory.DeleteUserAsync(user.ExternalUserId.Value.Value, context.CancellationToken);
                }

                user.CompleteDeprovisioning();
                await unitOfWork.SaveChangesAsync(context.CancellationToken);
            }
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }
}
