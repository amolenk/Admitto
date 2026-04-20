using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class ReconfirmPolicyTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly DateTimeOffset OpensAt = new(2025, 5, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ClosesAt = new(2025, 5, 25, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan SevenDays = TimeSpan.FromDays(7);

    [TestMethod]
    public void SC001_ReconfirmPolicy_Create_ValidInputs_Succeeds()
    {
        var sut = ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, SevenDays);

        sut.OpensAt.ShouldBe(OpensAt);
        sut.ClosesAt.ShouldBe(ClosesAt);
        sut.Cadence.ShouldBe(SevenDays);
    }

    [TestMethod]
    public void SC002_ReconfirmPolicy_Create_CloseBeforeOpen_Throws()
    {
        var result = ErrorResult.Capture(() =>
            ReconfirmPolicy.Create(DefaultEventId, ClosesAt, OpensAt, SevenDays));

        result.Error.ShouldMatch(ReconfirmPolicy.Errors.WindowCloseBeforeOpen);
    }

    [TestMethod]
    public void SC003_ReconfirmPolicy_Create_CloseEqualsOpen_Throws()
    {
        var result = ErrorResult.Capture(() =>
            ReconfirmPolicy.Create(DefaultEventId, OpensAt, OpensAt, SevenDays));

        result.Error.ShouldMatch(ReconfirmPolicy.Errors.WindowCloseBeforeOpen);
    }

    [TestMethod]
    public void SC004_ReconfirmPolicy_Create_CadenceZero_Throws()
    {
        var result = ErrorResult.Capture(() =>
            ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, TimeSpan.Zero));

        result.Error.ShouldMatch(ReconfirmPolicy.Errors.CadenceBelowMinimum);
    }

    [TestMethod]
    public void SC005_ReconfirmPolicy_Create_CadenceNegative_Throws()
    {
        var result = ErrorResult.Capture(() =>
            ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, TimeSpan.FromDays(-1)));

        result.Error.ShouldMatch(ReconfirmPolicy.Errors.CadenceBelowMinimum);
    }

    [TestMethod]
    public void SC006_ReconfirmPolicy_Create_CadenceBelowOneDay_Throws()
    {
        var result = ErrorResult.Capture(() =>
            ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, TimeSpan.FromHours(23)));

        result.Error.ShouldMatch(ReconfirmPolicy.Errors.CadenceBelowMinimum);
    }

    [TestMethod]
    public void SC007_ReconfirmPolicy_Create_CadenceExactlyOneDay_Succeeds()
    {
        var sut = ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, TimeSpan.FromDays(1));

        sut.Cadence.ShouldBe(TimeSpan.FromDays(1));
    }

    [TestMethod]
    public void SC008_ReconfirmPolicy_Update_ValidInputs_Succeeds()
    {
        var sut = ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, SevenDays);
        var newClosesAt = ClosesAt.AddDays(5);
        var newCadence = TimeSpan.FromDays(3);

        sut.Update(OpensAt, newClosesAt, newCadence);

        sut.ClosesAt.ShouldBe(newClosesAt);
        sut.Cadence.ShouldBe(newCadence);
    }

    [TestMethod]
    public void SC009_ReconfirmPolicy_Update_InvalidWindow_Throws()
    {
        var sut = ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, SevenDays);

        var result = ErrorResult.Capture(() => sut.Update(ClosesAt, OpensAt, SevenDays));

        result.Error.ShouldMatch(ReconfirmPolicy.Errors.WindowCloseBeforeOpen);
    }

    [TestMethod]
    public void SC010_ReconfirmPolicy_Update_InvalidCadence_Throws()
    {
        var sut = ReconfirmPolicy.Create(DefaultEventId, OpensAt, ClosesAt, SevenDays);

        var result = ErrorResult.Capture(() => sut.Update(OpensAt, ClosesAt, TimeSpan.Zero));

        result.Error.ShouldMatch(ReconfirmPolicy.Errors.CadenceBelowMinimum);
    }
}
