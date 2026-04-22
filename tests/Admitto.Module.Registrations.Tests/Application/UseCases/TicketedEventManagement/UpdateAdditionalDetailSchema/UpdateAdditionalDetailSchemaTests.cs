using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;

[TestClass]
public sealed class UpdateAdditionalDetailSchemaTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_UpdateAdditionalDetailSchema_ActiveEvent_PersistsSchema()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId,
            fixture.SeededVersion,
            [
                new UpdateAdditionalDetailSchemaCommand.FieldInput("dietary", "Dietary requirements", 200),
                new UpdateAdditionalDetailSchemaCommand.FieldInput("t-shirt-size", "T-shirt size", 10)
            ]);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
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

    [TestMethod]
    public async ValueTask SC002_UpdateAdditionalDetailSchema_EmptyList_ClearsSchema()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId,
            fixture.SeededVersion,
            []);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.AdditionalDetailSchema.Fields.Count.ShouldBe(0);
        });
    }

    [TestMethod]
    public async ValueTask SC003_UpdateAdditionalDetailSchema_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId,
            fixture.SeededVersion,
            [new UpdateAdditionalDetailSchemaCommand.FieldInput("dietary", "Dietary", 100)]);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public async ValueTask SC004_UpdateAdditionalDetailSchema_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId,
            fixture.SeededVersion,
            [new UpdateAdditionalDetailSchemaCommand.FieldInput("dietary", "Dietary", 100)]);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public async ValueTask SC005_UpdateAdditionalDetailSchema_VersionMismatch_ThrowsConcurrencyConflict()
    {
        var fixture = UpdateAdditionalDetailSchemaFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateAdditionalDetailSchemaCommand(
            fixture.EventId,
            fixture.SeededVersion + 99u,
            [new UpdateAdditionalDetailSchemaCommand.FieldInput("dietary", "Dietary", 100)]);

        var sut = new UpdateAdditionalDetailSchemaHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("concurrency_conflict");
    }
}
