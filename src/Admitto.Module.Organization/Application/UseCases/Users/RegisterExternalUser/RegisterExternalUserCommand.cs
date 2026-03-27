using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Users.RegisterExternalUser;

internal sealed record RegisterExternalUserCommand(Guid UserId) : Command;