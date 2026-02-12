using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership;

internal sealed record AssignTeamMembershipCommand(
    TeamId TeamId,
    EmailAddress EmailAddress,
    TeamMembershipRole Role)
    : Command;