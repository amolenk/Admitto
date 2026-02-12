using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.RegisterExternalUser;

internal sealed record RegisterExternalUserCommand(UserId UserId, EmailAddress EmailAddress) : Command;