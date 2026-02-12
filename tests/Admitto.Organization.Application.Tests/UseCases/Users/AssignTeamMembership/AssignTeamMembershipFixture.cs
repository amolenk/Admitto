using Amolenk.Admitto.Organization.Application.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Users.AssignTeamMembership;

internal sealed class AssignTeamMembershipFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public EmailAddress EmailAddress { get; } = EmailAddress.From("test@example.com");
    
    public UserId UserId { get; private set; }
    
    private AssignTeamMembershipFixture()
    {
    }

    public static AssignTeamMembershipFixture UserExists() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        // TODO Really reference Domain.Test? Or duplicate the builder / extract to Test Support
        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress)
            .Build();
        
        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });
        
        UserId = user.Id;
    }
}