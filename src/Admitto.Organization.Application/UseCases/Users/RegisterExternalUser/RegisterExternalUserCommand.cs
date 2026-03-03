using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.RegisterExternalUser;

internal sealed record RegisterExternalUserCommand(Guid UserId) : Command;