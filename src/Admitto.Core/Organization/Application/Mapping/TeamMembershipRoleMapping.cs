using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.Mapping;

internal static class TeamMembershipRoleMapping
{
    public static TeamMembershipRole ToDomain(this TeamMembershipRoleDto dto) => dto switch
    {
        TeamMembershipRoleDto.Crew => TeamMembershipRole.Crew,
        TeamMembershipRoleDto.Organizer => TeamMembershipRole.Organizer,
        TeamMembershipRoleDto.Owner => TeamMembershipRole.Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(dto))
    };
    
    public static TeamMembershipRoleDto ToDto(this TeamMembershipRole domain) => domain switch
    {
        TeamMembershipRole.Crew => TeamMembershipRoleDto.Crew,
        TeamMembershipRole.Organizer => TeamMembershipRoleDto.Organizer,
        TeamMembershipRole.Owner => TeamMembershipRoleDto.Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(domain))
    };

}