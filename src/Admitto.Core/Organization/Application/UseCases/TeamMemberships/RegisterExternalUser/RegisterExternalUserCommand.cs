using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RegisterExternalUser;

internal sealed record RegisterExternalUserCommand(Guid UserId) : Command;