using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Data;

public static class UserDataFactory
{
    public const string TestUserEmail = "alice@example.com";
    
    // TODO Remove if not needed
    public static readonly Guid TestUserId = new ("236d597b-a4df-4e08-b90c-b4cb1808ec2d");
    
    public static TeamMember CreateTeamMember(string? email = null, TeamMemberRole? role = null)
    {
        email ??= "bob@example.com";
        role ??= TeamMemberRole.Manager;

        return TeamMember.Create(email, role);
    }
}