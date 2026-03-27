using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.Users.AssignTeamMembership;

internal sealed class AssignTeamMembershipFixture
{
    public Guid TeamId { get; } = Guid.NewGuid();
    public string EmailAddress { get; } = "test@example.com";
    public Guid UserId { get; private set; }
    
    private AssignTeamMembershipFixture()
    {
    }

    public static AssignTeamMembershipFixture UserExists() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        // TODO Really reference Domain.Test? Or duplicate the builder / extract to Test Support
        var user = new UserBuilder()
            .WithEmailAddress(Module.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .Build();
        
        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });
        
        UserId = user.Id.Value;
    }
}