using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class TicketedEventTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly Slug DefaultSlug = Slug.From("my-event");
    private static readonly DisplayName DefaultName = DisplayName.From("My Event");
    private static readonly DateTimeOffset DefaultStart = new(2030, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DefaultEnd = new(2030, 6, 1, 17, 0, 0, TimeSpan.Zero);
    private static readonly AbsoluteUrl DefaultWebsite = AbsoluteUrl.From("https://example.com");
    private static readonly AbsoluteUrl DefaultBaseUrl = AbsoluteUrl.From("https://tickets.example.com");

    // ── Create ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void SC001_Create_ValidDates_ReturnsActiveEvent()
    {
        var sut = NewEvent();

        sut.Id.ShouldBe(DefaultEventId);
        sut.TeamId.ShouldBe(DefaultTeamId);
        sut.Slug.ShouldBe(DefaultSlug);
        sut.Name.ShouldBe(DefaultName);
        sut.Status.ShouldBe(EventLifecycleStatus.Active);
        sut.IsActive.ShouldBeTrue();
        sut.RegistrationPolicy.ShouldBeNull();
        sut.CancellationPolicy.ShouldBeNull();
        sut.ReconfirmPolicy.ShouldBeNull();
    }

    [TestMethod]
    public void SC002_Create_EndBeforeStart_Throws()
    {
        var act = () => TicketedEvent.Create(
            DefaultEventId,
            DefaultTeamId,
            DefaultSlug,
            DefaultName,
            DefaultWebsite,
            DefaultBaseUrl,
            DefaultEnd,
            DefaultStart);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.end_before_start");
    }

    // ── UpdateDetails ────────────────────────────────────────────────────────

    [TestMethod]
    public void SC010_UpdateDetails_Active_UpdatesFields()
    {
        var sut = NewEvent();
        var newName = DisplayName.From("Renamed Event");
        var newStart = DefaultStart.AddDays(1);
        var newEnd = DefaultEnd.AddDays(1);

        sut.UpdateDetails(newName, DefaultWebsite, DefaultBaseUrl, newStart, newEnd);

        sut.Name.ShouldBe(newName);
        sut.StartsAt.ShouldBe(newStart);
        sut.EndsAt.ShouldBe(newEnd);
    }

    [TestMethod]
    public void SC011_UpdateDetails_EndBeforeStart_Throws()
    {
        var sut = NewEvent();

        var act = () => sut.UpdateDetails(DefaultName, DefaultWebsite, DefaultBaseUrl, DefaultEnd, DefaultStart);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.end_before_start");
    }

    [TestMethod]
    public void SC012_UpdateDetails_NotActive_Throws()
    {
        var sut = NewEvent();
        sut.Cancel();

        var act = () => sut.UpdateDetails(DefaultName, DefaultWebsite, DefaultBaseUrl, DefaultStart, DefaultEnd);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void SC020_Cancel_Active_TransitionsToCancelledAndRaisesEvent()
    {
        var sut = NewEvent();

        sut.Cancel();

        sut.Status.ShouldBe(EventLifecycleStatus.Cancelled);
        sut.IsActive.ShouldBeFalse();

        var raised = sut.GetDomainEvents()
            .OfType<TicketedEventStatusChangedDomainEvent>()
            .ShouldHaveSingleItem();
        raised.TicketedEventId.ShouldBe(DefaultEventId);
        raised.TeamId.ShouldBe(DefaultTeamId);
        raised.Slug.ShouldBe(DefaultSlug);
        raised.NewStatus.ShouldBe(EventLifecycleStatus.Cancelled);
    }

    [TestMethod]
    public void SC021_Cancel_AlreadyCancelled_Throws()
    {
        var sut = NewEvent();
        sut.Cancel();

        var ex = Should.Throw<BusinessRuleViolationException>(() => sut.Cancel());
        ex.Error.Code.ShouldBe("ticketed_event.already_cancelled");
    }

    [TestMethod]
    public void SC022_Cancel_AlreadyArchived_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var ex = Should.Throw<BusinessRuleViolationException>(() => sut.Cancel());
        ex.Error.Code.ShouldBe("ticketed_event.already_archived");
    }

    // ── Archive ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void SC030_Archive_Active_TransitionsToArchivedAndRaisesEvent()
    {
        var sut = NewEvent();

        sut.Archive();

        sut.Status.ShouldBe(EventLifecycleStatus.Archived);
        var raised = sut.GetDomainEvents()
            .OfType<TicketedEventStatusChangedDomainEvent>()
            .ShouldHaveSingleItem();
        raised.NewStatus.ShouldBe(EventLifecycleStatus.Archived);
    }

    [TestMethod]
    public void SC031_Archive_Cancelled_TransitionsToArchived()
    {
        var sut = NewEvent();
        sut.Cancel();
        sut.ClearDomainEvents();

        sut.Archive();

        sut.Status.ShouldBe(EventLifecycleStatus.Archived);
        sut.GetDomainEvents()
            .OfType<TicketedEventStatusChangedDomainEvent>()
            .ShouldHaveSingleItem()
            .NewStatus.ShouldBe(EventLifecycleStatus.Archived);
    }

    [TestMethod]
    public void SC032_Archive_AlreadyArchived_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var ex = Should.Throw<BusinessRuleViolationException>(() => sut.Archive());
        ex.Error.Code.ShouldBe("ticketed_event.already_archived");
    }

    // ── ConfigureRegistrationPolicy ──────────────────────────────────────────

    [TestMethod]
    public void SC040_ConfigureRegistrationPolicy_Active_StoresPolicy()
    {
        var sut = NewEvent();
        var policy = NewRegistrationPolicy();

        sut.ConfigureRegistrationPolicy(policy);

        sut.RegistrationPolicy.ShouldBe(policy);
    }

    [TestMethod]
    public void SC041_ConfigureRegistrationPolicy_Cancelled_Throws()
    {
        var sut = NewEvent();
        sut.Cancel();

        var act = () => sut.ConfigureRegistrationPolicy(NewRegistrationPolicy());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public void SC042_ConfigureRegistrationPolicy_Archived_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var act = () => sut.ConfigureRegistrationPolicy(NewRegistrationPolicy());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    // ── ConfigureCancellationPolicy ──────────────────────────────────────────

    [TestMethod]
    public void SC050_ConfigureCancellationPolicy_Active_StoresPolicy()
    {
        var sut = NewEvent();
        var policy = new TicketedEventCancellationPolicy(DefaultStart.AddDays(-3));

        sut.ConfigureCancellationPolicy(policy);

        sut.CancellationPolicy.ShouldBe(policy);
    }

    [TestMethod]
    public void SC051_ConfigureCancellationPolicy_NotActive_Throws()
    {
        var sut = NewEvent();
        sut.Cancel();

        var act = () => sut.ConfigureCancellationPolicy(
            new TicketedEventCancellationPolicy(DefaultStart));

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    // ── ConfigureReconfirmPolicy ─────────────────────────────────────────────

    [TestMethod]
    public void SC060_ConfigureReconfirmPolicy_Active_StoresPolicy()
    {
        var sut = NewEvent();
        var policy = NewReconfirmPolicy();

        sut.ConfigureReconfirmPolicy(policy);

        sut.ReconfirmPolicy.ShouldBe(policy);
    }

    [TestMethod]
    public void SC061_ConfigureReconfirmPolicy_NotActive_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var act = () => sut.ConfigureReconfirmPolicy(NewReconfirmPolicy());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    // ── UpdateAdditionalDetailSchema ─────────────────────────────────────────

    [TestMethod]
    public void SC100_UpdateAdditionalDetailSchema_Active_StoresSchemaAndRaisesEvent()
    {
        var sut = NewEvent();
        var fields = new[]
        {
            AdditionalDetailField.Create("dietary", "Dietary requirements", 200),
            AdditionalDetailField.Create("tshirt", "T-shirt size", 5),
        };

        sut.UpdateAdditionalDetailSchema(fields);

        sut.AdditionalDetailSchema.Fields.Count.ShouldBe(2);
        sut.AdditionalDetailSchema.Fields[0].Key.ShouldBe("dietary");
        sut.AdditionalDetailSchema.Fields[1].Key.ShouldBe("tshirt");

        var raised = sut.GetDomainEvents()
            .OfType<AdditionalDetailSchemaUpdatedDomainEvent>()
            .ShouldHaveSingleItem();
        raised.TicketedEventId.ShouldBe(DefaultEventId);
        raised.Schema.Fields.Count.ShouldBe(2);
    }

    [TestMethod]
    public void SC101_UpdateAdditionalDetailSchema_ReplacesAtomically()
    {
        var sut = NewEvent();
        sut.UpdateAdditionalDetailSchema(new[]
        {
            AdditionalDetailField.Create("dietary", "Dietary requirements", 200),
        });

        sut.UpdateAdditionalDetailSchema(new[]
        {
            AdditionalDetailField.Create("tshirt", "T-shirt size", 5),
        });

        sut.AdditionalDetailSchema.Fields.Count.ShouldBe(1);
        sut.AdditionalDetailSchema.Fields[0].Key.ShouldBe("tshirt");
    }

    [TestMethod]
    public void SC102_UpdateAdditionalDetailSchema_Cancelled_Throws()
    {
        var sut = NewEvent();
        sut.Cancel();

        var act = () => sut.UpdateAdditionalDetailSchema(Array.Empty<AdditionalDetailField>());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public void SC103_UpdateAdditionalDetailSchema_Archived_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var act = () => sut.UpdateAdditionalDetailSchema(Array.Empty<AdditionalDetailField>());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public void SC104_UpdateAdditionalDetailSchema_DuplicateKey_Throws()
    {
        var sut = NewEvent();

        var act = () => sut.UpdateAdditionalDetailSchema(new[]
        {
            AdditionalDetailField.Create("dietary", "Dietary", 200),
            AdditionalDetailField.Create("dietary", "Dietary needs", 200),
        });

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("additional_detail_schema.duplicate_key");
    }

    [TestMethod]
    public void SC105_UpdateAdditionalDetailSchema_DuplicateName_CaseInsensitive_Throws()
    {
        var sut = NewEvent();

        var act = () => sut.UpdateAdditionalDetailSchema(new[]
        {
            AdditionalDetailField.Create("dietary-1", "Dietary", 200),
            AdditionalDetailField.Create("dietary-2", "dietary", 200),
        });

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("additional_detail_schema.duplicate_name");
    }

    [TestMethod]
    public void SC106_UpdateAdditionalDetailSchema_TooManyFields_Throws()
    {
        var sut = NewEvent();
        var fields = Enumerable.Range(0, AdditionalDetailSchema.MaxFields + 1)
            .Select(i => AdditionalDetailField.Create($"f-{i}", $"Field {i}", 100))
            .ToArray();

        var act = () => sut.UpdateAdditionalDetailSchema(fields);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("additional_detail_schema.too_many_fields");
    }

    // ── IsRegistrationOpen ───────────────────────────────────────────────────

    [TestMethod]
    public void SC070_IsRegistrationOpen_NoPolicy_ReturnsFalse()
    {
        var sut = NewEvent();

        sut.IsRegistrationOpen(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [TestMethod]
    public void SC071_IsRegistrationOpen_InsideWindowAndActive_ReturnsTrue()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-1), now.AddDays(1)));

        sut.IsRegistrationOpen(now).ShouldBeTrue();
    }

    [TestMethod]
    public void SC072_IsRegistrationOpen_Cancelled_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-1), now.AddDays(1)));
        sut.Cancel();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC073_IsRegistrationOpen_BeforeWindow_Active_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(1), now.AddDays(2)));

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC074_IsRegistrationOpen_AfterWindow_Active_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-2), now.AddDays(-1)));

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC075_IsRegistrationOpen_InsideWindow_Archived_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-1), now.AddDays(1)));
        sut.Archive();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC076_IsRegistrationOpen_BeforeWindow_Cancelled_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(1), now.AddDays(2)));
        sut.Cancel();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC077_IsRegistrationOpen_AfterWindow_Cancelled_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-2), now.AddDays(-1)));
        sut.Cancel();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC078_IsRegistrationOpen_BeforeWindow_Archived_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(1), now.AddDays(2)));
        sut.Archive();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC079_IsRegistrationOpen_AfterWindow_Archived_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-2), now.AddDays(-1)));
        sut.Archive();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    // ── Value object: RegistrationPolicy ─────────────────────────────────────

    [TestMethod]
    public void SC080_RegistrationPolicy_CloseBeforeOpen_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventRegistrationPolicy.Create(now.AddDays(1), now);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event_registration_policy.window_close_before_open");
    }

    [TestMethod]
    public void SC081_RegistrationPolicy_CloseEqualsOpen_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventRegistrationPolicy.Create(now, now);

        Should.Throw<BusinessRuleViolationException>(act);
    }

    [TestMethod]
    public void SC082_RegistrationPolicy_EmailDomain_MatchesCaseInsensitively()
    {
        var now = DateTimeOffset.UtcNow;
        var policy = TicketedEventRegistrationPolicy.Create(
            now.AddDays(-1), now.AddDays(1), "@Acme.com");

        policy.IsEmailDomainAllowed("user@acme.com").ShouldBeTrue();
        policy.IsEmailDomainAllowed("user@other.com").ShouldBeFalse();
    }

    // ── Value object: ReconfirmPolicy ────────────────────────────────────────

    [TestMethod]
    public void SC090_ReconfirmPolicy_CloseBeforeOpen_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventReconfirmPolicy.Create(
            now.AddDays(2), now, TimeSpan.FromDays(1));

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event_reconfirm_policy.window_close_before_open");
    }

    [TestMethod]
    public void SC091_ReconfirmPolicy_CadenceBelowOneDay_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventReconfirmPolicy.Create(
            now, now.AddDays(10), TimeSpan.FromHours(23));

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event_reconfirm_policy.cadence_below_minimum");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TicketedEvent NewEvent() => TicketedEvent.Create(
        DefaultEventId, DefaultTeamId, DefaultSlug, DefaultName, DefaultWebsite, DefaultBaseUrl, DefaultStart, DefaultEnd);

    private static TicketedEventRegistrationPolicy NewRegistrationPolicy()
    {
        var now = DateTimeOffset.UtcNow;
        return TicketedEventRegistrationPolicy.Create(now.AddDays(-1), now.AddDays(30));
    }

    private static TicketedEventReconfirmPolicy NewReconfirmPolicy()
    {
        var now = DateTimeOffset.UtcNow;
        return TicketedEventReconfirmPolicy.Create(
            now.AddDays(-10), now.AddDays(-1), TimeSpan.FromDays(2));
    }
}
