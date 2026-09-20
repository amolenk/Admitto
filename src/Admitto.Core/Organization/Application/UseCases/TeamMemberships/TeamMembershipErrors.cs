using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships;

/// <summary>
/// Errors shared across the team-membership use cases that look up a <see cref="Domain.Entities.User"/>
/// by email address as an application-level precondition.
/// </summary>
internal static class TeamMembershipErrors
{
    public static Error UserNotFound(EmailAddress email) =>
        new(
            "user.not_found",
            "No user with the specified email address exists.",
            Type: ErrorType.NotFound,
            Details: new Dictionary<string, object?> { ["email"] = email.Value });
}
