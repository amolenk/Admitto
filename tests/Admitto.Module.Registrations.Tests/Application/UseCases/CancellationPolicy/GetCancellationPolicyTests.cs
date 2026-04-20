using Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.GetCancellationPolicy;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

using CancellationPolicyEntity = Amolenk.Admitto.Module.Registrations.Domain.Entities.CancellationPolicy;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CancellationPolicy;

[TestClass]
public sealed class GetCancellationPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Get existing policy → returns DTO with correct cutoff
    [TestMethod]
    public async ValueTask SC001_GetCancellationPolicy_PolicyExists_ReturnsDtoWithCorrectCutoff()
    {
        // Arrange
        var eventId = TicketedEventId.New();
        var cutoff = new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero);

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.CancellationPolicies.Add(
                CancellationPolicyEntity.Create(eventId, cutoff));
        });

        var query = new GetCancellationPolicyQuery(eventId);
        var sut = new GetCancellationPolicyHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.LateCancellationCutoff.ShouldBe(cutoff);
    }

    // SC002: Get when no policy exists → returns null
    [TestMethod]
    public async ValueTask SC002_GetCancellationPolicy_NoPolicyExists_ReturnsNull()
    {
        // Arrange
        var eventId = TicketedEventId.New();
        var query = new GetCancellationPolicyQuery(eventId);
        var sut = new GetCancellationPolicyHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }
}
