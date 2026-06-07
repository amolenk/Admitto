using System.Security.Claims;
using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Api.Auth;

/// <summary>
/// Resolves the calling user's domain identity from JWT claims, performing lazy ExternalUserId binding
/// on the first authenticated request.
///
/// Resolution order:
/// 1. Find User by ExternalUserId matching the JWT <c>sub</c> claim → direct hit.
/// 2. Find User by email → bind <c>ExternalUserId</c> when it is null, then return.
/// 3. If neither resolves → return null (caller gets 403).
/// </summary>
public sealed class UserContextResolver(
    IOrganizationWriteStore writeStore,
    [FromKeyedServices(OrganizationModule.Key)]
    IUnitOfWork unitOfWork)
{
    public async ValueTask<UserContextDto?> ResolveAsync(
        ClaimsPrincipal principal,
        TeamId? teamId,
        TicketedEventId? eventId,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name") ?? "Unknown";

        if (string.IsNullOrWhiteSpace(sub))
            return null;

        var externalId = ExternalUserId.From(sub);

        // Run a single query to get user, memberships and check if the event actually belongs to the team
        var result = await writeStore.Users
            .Where(u => u.ExternalUserId == externalId)
            .Select(u => new
            {
                u.Id,
                u.EmailAddress,
                u.IsAdmin,
                u.ExternalUserId,
                Memberships = u.Memberships.Select(m => new { m.TeamId, m.Role }).ToList(),
                EventBelongsToTeam = teamId == null || eventId == null ||
                                     writeStore.Teams.Any(t =>
                                         t.Id == teamId &&
                                         t.EventCreationRequests.Any(ecr => ecr.TicketedEventId == eventId))
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (result is not null)
        {
            // Event-scope guard: eventId was provided but doesn't belong to the team.
            // Administrators are exempt — they have unrestricted access across all teams and events.
            if (!result.IsAdmin && !result.EventBelongsToTeam)
                return null;

            return new UserContextDto(
                result.Id.Value,
                name,
                result.EmailAddress.Value,
                result.Memberships.Select(m => m.Role).SingleOrDefault(),
                result.IsAdmin);
        }

        // First sign-in: fall back to email
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var emailAddress = EmailAddress.From(email);
        var byEmail = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (byEmail is null || byEmail.ExternalUserId is not null)
            return null;

        byEmail.AssignExternalUserId(externalId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserContextDto(
            byEmail.Id.Value,
            name,
            byEmail.EmailAddress.Value,
            byEmail.Memberships.Select(m => m.Role).SingleOrDefault(),
            byEmail.IsAdmin);
    }
}
