using Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.GetReconfirmPolicy;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.ReconfirmPolicy;

[TestClass]
public sealed class GetReconfirmPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_GetReconfirmPolicy_ExistingPolicy_ReturnsDtoWithCorrectFields()
    {
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var cadence = TimeSpan.FromDays(3);

        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = Domain.Entities.ReconfirmPolicy.Create(eventId, opensAt, closesAt, cadence);
            dbContext.ReconfirmPolicies.Add(policy);
        });

        var query = new GetReconfirmPolicyQuery(eventId);
        var sut = new GetReconfirmPolicyHandler(Environment.Database.Context);

        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.OpensAt.ShouldBe(opensAt);
        result.ClosesAt.ShouldBe(closesAt);
        result.CadenceDays.ShouldBe(3);
    }

    [TestMethod]
    public async ValueTask SC002_GetReconfirmPolicy_NoExistingPolicy_ReturnsNull()
    {
        var eventId = TicketedEventId.New();

        var query = new GetReconfirmPolicyQuery(eventId);
        var sut = new GetReconfirmPolicyHandler(Environment.Database.Context);

        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        result.ShouldBeNull();
    }
}
