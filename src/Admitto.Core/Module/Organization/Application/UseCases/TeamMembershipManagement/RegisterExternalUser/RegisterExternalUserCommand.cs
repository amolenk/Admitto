using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;

internal sealed record RegisterExternalUserCommand(Guid UserId) : Command;