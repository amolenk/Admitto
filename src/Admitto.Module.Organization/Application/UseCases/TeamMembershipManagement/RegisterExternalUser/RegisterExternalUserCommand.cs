using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;

internal sealed record RegisterExternalUserCommand(Guid UserId) : Command;