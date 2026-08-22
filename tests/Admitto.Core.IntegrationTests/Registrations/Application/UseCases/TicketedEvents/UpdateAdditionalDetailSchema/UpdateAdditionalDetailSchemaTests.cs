using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateAdditionalDetailSchema;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.UpdateAdditionalDetailSchema;

[TestClass]
public sealed class UpdateAdditionalDetailSchemaTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active ticketed event
    // When the additional detail schema is updated with new fields
    // Then the fields are persisted on the event in the given order
    [TestMethod]
    public async ValueTask UpdateAdditionalDetailSchema_ActiveEvent_PersistsSchema()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            [
                new UpdateAdditionalDetailSchemaCommand.FieldInput("dietary", "Dietary requirements", 200),
                new UpdateAdditionalDetailSchemaCommand.FieldInput("t-shirt-size", "T-shirt size", 10)
            ]);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.AdditionalDetailSchema.Fields.Count.ShouldBe(2);
            te.AdditionalDetailSchema.Fields[0].Key.ShouldBe("dietary");
            te.AdditionalDetailSchema.Fields[0].Name.ShouldBe("Dietary requirements");
            te.AdditionalDetailSchema.Fields[0].MaxLength.ShouldBe(200);
            te.AdditionalDetailSchema.Fields[1].Key.ShouldBe("t-shirt-size");
        });
    }

    // Given an active ticketed event
    // When the additional detail schema is updated with an empty field list
    // Then the event's additional detail schema is cleared
    [TestMethod]
    public async ValueTask UpdateAdditionalDetailSchema_EmptyList_ClearsSchema()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            []);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.AdditionalDetailSchema.Fields.Count.ShouldBe(0);
        });
    }

    // Given an archived ticketed event
    // When the additional detail schema is updated
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask UpdateAdditionalDetailSchema_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            [new UpdateAdditionalDetailSchemaCommand.FieldInput("dietary", "Dietary", 100)]);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }

    // Given an active ticketed event
    // When the additional detail schema is updated with a stale version number
    // Then it fails with a concurrency conflict error
    [TestMethod]
    public async ValueTask UpdateAdditionalDetailSchema_VersionMismatch_ThrowsConcurrencyConflict()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion + 99u,
            [new UpdateAdditionalDetailSchemaCommand.FieldInput("dietary", "Dietary", 100)]);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ConcurrencyConflictError.Create(fixture.SeededVersion + 99u, fixture.SeededVersion));
    }
}
