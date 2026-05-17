using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Shared.Contracts;

public sealed record UserContextTeamMembershipDto(Guid TeamId, TeamMembershipRole Role);
