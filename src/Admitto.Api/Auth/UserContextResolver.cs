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
/// 3. If neither resolves, or a stored ExternalUserId doesn't match the sub → return null (caller gets 403).
/// </summary>
public sealed class UserContextResolver(
    IOrganizationWriteStore writeStore,
    [FromKeyedServices(OrganizationModule.Key)] IUnitOfWork unitOfWork)
{
    public async ValueTask<UserContextDto?> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name") ?? "Unknown";

        if (string.IsNullOrWhiteSpace(sub))
            return null;

        // 1. Resolve by ExternalUserId (fast path after first sign-in).
        var externalId = ExternalUserId.From(sub);
        var byExternalId = await writeStore.Users
            .FirstOrDefaultAsync(u => u.ExternalUserId == externalId, cancellationToken);

        if (byExternalId is not null)
            return new UserContextDto(
                byExternalId.Id.Value,
                name,
                byExternalId.EmailAddress.Value,
                byExternalId.IsAdmin,
                byExternalId.Memberships.Select(m => new UserContextTeamMembershipDto(m.Id.Value, m.Role)).ToList());

        // 2. Fall back to email (first sign-in: bind the sub to the pre-invited user).
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var emailAddress = EmailAddress.From(email);
        var byEmail = await writeStore.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (byEmail is null)
            return null;

        // If the user already has a different sub stored, reject — possible account takeover.
        if (byEmail.ExternalUserId is not null)
            return null;

        // Bind the sub and persist.
        byEmail.AssignExternalUserId(ExternalUserId.From(sub));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserContextDto(
            byEmail.Id.Value,
            name,
            byEmail.EmailAddress.Value,
            byEmail.IsAdmin,
            byEmail.Memberships.Select(m => new UserContextTeamMembershipDto(m.Id.Value, m.Role)).ToList());
    }
}
