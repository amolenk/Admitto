using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class TicketedEventTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly EventName DefaultName = EventName.From("My Event");
    private static readonly DateTimeOffset DefaultStart = new(2030, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DefaultEnd = new(2030, 6, 1, 17, 0, 0, TimeSpan.Zero);
    private static readonly AbsoluteUrl DefaultWebsite = AbsoluteUrl.From("https://example.com");
    private static readonly AbsoluteUrl DefaultBaseUrl = AbsoluteUrl.From("https://tickets.example.com");

    // ── Create ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_ValidDates_ReturnsActiveEvent()
    {
        var sut = NewEvent();

        sut.Id.ShouldBe(DefaultEventId);
        sut.TeamId.ShouldBe(DefaultTeamId);
        sut.Name.ShouldBe(DefaultName);
        sut.Status.ShouldBe(EventLifecycleStatus.Active);
        sut.IsActive.ShouldBeTrue();
        sut.RegistrationPolicy.ShouldBeNull();
        sut.ReconfirmPolicy.ShouldBeNull();
    }

    [TestMethod]
    public void Create_GeneratesSigningKey_DecodingToAtLeast32Bytes()
    {
        var sut = NewEvent();

        sut.SigningKey.ShouldNotBeNullOrWhiteSpace();
        Convert.FromBase64String(sut.SigningKey).Length.ShouldBeGreaterThanOrEqualTo(32);
    }

    [TestMethod]
    public void Create_TwoEvents_HaveDifferentSigningKeys()
    {
        var first = NewEvent();
        var second = TicketedEvent.Create(
        CreationRequestId.From(Guid.NewGuid()),
            TicketedEventId.New(),
            DefaultTeamId,
            DefaultName,
            DefaultWebsite,
            DefaultBaseUrl,
            DefaultStart,
            DefaultEnd,
            TimeZoneId.From("UTC"));

        second.SigningKey.ShouldNotBe(first.SigningKey);
    }

    [TestMethod]
    public void Create_EndBeforeStart_Throws()
    {
        var act = () => TicketedEvent.Create(
        CreationRequestId.From(Guid.NewGuid()),
            DefaultEventId,
            DefaultTeamId,
            DefaultName,
            DefaultWebsite,
            DefaultBaseUrl,
            DefaultEnd,
            DefaultStart,
                TimeZoneId.From("UTC"));

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.end_before_start");
    }

    // ── UpdateDetails ────────────────────────────────────────────────────────

    [TestMethod]
    public void UpdateDetails_Active_UpdatesFields()
    {
        var sut = NewEvent();
        var newName = EventName.From("Renamed Event");
        var newStart = DefaultStart.AddDays(1);
        var newEnd = DefaultEnd.AddDays(1);

        sut.UpdateDetails(newName, DefaultWebsite, DefaultBaseUrl, newStart, newEnd);

        sut.Name.ShouldBe(newName);
        sut.StartsAt.ShouldBe(newStart);
        sut.EndsAt.ShouldBe(newEnd);
    }

    [TestMethod]
    public void UpdateDetails_EndBeforeStart_Throws()
    {
        var sut = NewEvent();

        var act = () => sut.UpdateDetails(DefaultName, DefaultWebsite, DefaultBaseUrl, DefaultEnd, DefaultStart);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.end_before_start");
    }

    [TestMethod]
    public void UpdateDetails_NotActive_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var act = () => sut.UpdateDetails(DefaultName, DefaultWebsite, DefaultBaseUrl, DefaultStart, DefaultEnd);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    // ── Archive ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Archive_Active_TransitionsToArchivedAndRaisesEvent()
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
    public void Archive_AlreadyArchived_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var ex = Should.Throw<BusinessRuleViolationException>(() => sut.Archive());
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    // ── ConfigureRegistrationPolicy ──────────────────────────────────────────

    [TestMethod]
    public void ConfigureRegistrationPolicy_Active_StoresPolicy()
    {
        var sut = NewEvent();
        var policy = NewRegistrationPolicy();

        sut.ConfigureRegistrationPolicy(policy);

        sut.RegistrationPolicy.ShouldBe(policy);
    }

    [TestMethod]
    public void ConfigureRegistrationPolicy_Archived_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var act = () => sut.ConfigureRegistrationPolicy(NewRegistrationPolicy());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public void ConfigureRegistrationPolicy_WindowClosesAtEventEnd_Accepted()
    {
        var sut = NewEvent();
        var policy = TicketedEventRegistrationPolicy.Create(DefaultStart.AddDays(-30), DefaultEnd);

        sut.ConfigureRegistrationPolicy(policy);

        sut.RegistrationPolicy.ShouldBe(policy);
    }

    [TestMethod]
    public void ConfigureRegistrationPolicy_WindowClosesAfterEventEnd_Throws()
    {
        var sut = NewEvent();
        var policy = TicketedEventRegistrationPolicy.Create(DefaultStart.AddDays(-30), DefaultEnd.AddSeconds(1));

        var act = () => sut.ConfigureRegistrationPolicy(policy);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.registration_window_closes_after_event_end");
    }

    // ── ConfigureReconfirmPolicy ─────────────────────────────────────────────

    [TestMethod]
    public void ConfigureReconfirmPolicy_Active_StoresPolicy()
    {
        var sut = NewEvent();
        var policy = NewReconfirmPolicy();

        sut.ConfigureReconfirmPolicy(policy);

        sut.ReconfirmPolicy.ShouldBe(policy);
    }

    [TestMethod]
    public void ConfigureReconfirmPolicy_NotActive_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var act = () => sut.ConfigureReconfirmPolicy(NewReconfirmPolicy());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public void ConfigureReconfirmPolicy_WindowClosesBeforeEventStart_Accepted()
    {
        var sut = NewEvent();
        var policy = TicketedEventReconfirmPolicy.Create(
            DefaultStart.AddDays(-60), DefaultStart.AddSeconds(-1), TimeSpan.FromDays(2), TimeSpan.FromHours(24));

        sut.ConfigureReconfirmPolicy(policy);

        sut.ReconfirmPolicy.ShouldBe(policy);
    }

    [TestMethod]
    public void ConfigureReconfirmPolicy_WindowClosesAtEventStart_Throws()
    {
        var sut = NewEvent();
        var policy = TicketedEventReconfirmPolicy.Create(
            DefaultStart.AddDays(-60), DefaultStart, TimeSpan.FromDays(2), TimeSpan.FromHours(24));

        var act = () => sut.ConfigureReconfirmPolicy(policy);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.reconfirm_window_closes_after_event_start");
    }

    [TestMethod]
    public void ConfigureReconfirmPolicy_WindowClosesAfterEventStart_Throws()
    {
        var sut = NewEvent();
        var policy = TicketedEventReconfirmPolicy.Create(
            DefaultStart.AddDays(-60), DefaultStart.AddSeconds(1), TimeSpan.FromDays(2), TimeSpan.FromHours(24));

        var act = () => sut.ConfigureReconfirmPolicy(policy);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.reconfirm_window_closes_after_event_start");
    }

    [TestMethod]
    public void ConfigureReconfirmPolicy_NullPolicy_Accepted()
    {
        var sut = NewEvent();
        sut.ConfigureReconfirmPolicy(NewReconfirmPolicy());

        sut.ConfigureReconfirmPolicy(null);

        sut.ReconfirmPolicy.ShouldBeNull();
    }

    // ── UpdateAdditionalDetailSchema ─────────────────────────────────────────

    [TestMethod]
    public void UpdateAdditionalDetailSchema_Active_StoresSchemaAndRaisesEvent()
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
    public void UpdateAdditionalDetailSchema_ReplacesAtomically()
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
    public void UpdateAdditionalDetailSchema_Archived_Throws()
    {
        var sut = NewEvent();
        sut.Archive();

        var act = () => sut.UpdateAdditionalDetailSchema(Array.Empty<AdditionalDetailField>());

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public void UpdateAdditionalDetailSchema_DuplicateKey_Throws()
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
    public void UpdateAdditionalDetailSchema_DuplicateName_CaseInsensitive_Throws()
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
    public void UpdateAdditionalDetailSchema_TooManyFields_Throws()
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
    public void IsRegistrationOpen_NoPolicy_ReturnsFalse()
    {
        var sut = NewEvent();

        sut.IsRegistrationOpen(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [TestMethod]
    public void IsRegistrationOpen_InsideWindowAndActive_ReturnsTrue()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-1), now.AddDays(1)));

        sut.IsRegistrationOpen(now).ShouldBeTrue();
    }

    [TestMethod]
    public void IsRegistrationOpen_BeforeWindow_Active_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(1), now.AddDays(2)));

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void IsRegistrationOpen_AfterWindow_Active_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-2), now.AddDays(-1)));

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void IsRegistrationOpen_InsideWindow_Archived_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(-1), now.AddDays(1)));
        sut.Archive();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void IsRegistrationOpen_BeforeWindow_Archived_ReturnsFalse()
    {
        var sut = NewEvent();
        var now = DateTimeOffset.UtcNow;
        sut.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            now.AddDays(1), now.AddDays(2)));
        sut.Archive();

        sut.IsRegistrationOpen(now).ShouldBeFalse();
    }

    [TestMethod]
    public void IsRegistrationOpen_AfterWindow_Archived_ReturnsFalse()
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
    public void RegistrationPolicy_CloseBeforeOpen_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventRegistrationPolicy.Create(now.AddDays(1), now);

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event_registration_policy.window_close_before_open");
    }

    [TestMethod]
    public void RegistrationPolicy_CloseEqualsOpen_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventRegistrationPolicy.Create(now, now);

        Should.Throw<BusinessRuleViolationException>(act);
    }

    [TestMethod]
    public void RegistrationPolicy_EmailDomain_MatchesCaseInsensitively()
    {
        var now = DateTimeOffset.UtcNow;
        var policy = TicketedEventRegistrationPolicy.Create(
            now.AddDays(-1), now.AddDays(1), "@Acme.com");

        policy.IsEmailDomainAllowed("user@acme.com").ShouldBeTrue();
        policy.IsEmailDomainAllowed("user@other.com").ShouldBeFalse();
    }

    // ── Value object: ReconfirmPolicy ────────────────────────────────────────

    [TestMethod]
    public void ReconfirmPolicy_CloseBeforeOpen_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventReconfirmPolicy.Create(
            now.AddDays(2), now, TimeSpan.FromDays(1), TimeSpan.FromHours(24));

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event_reconfirm_policy.window_close_before_open");
    }

    [TestMethod]
    public void ReconfirmPolicy_CadenceBelowOneHour_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => TicketedEventReconfirmPolicy.Create(
            now, now.AddDays(10), TimeSpan.FromMinutes(59), TimeSpan.FromHours(24));

        var ex = Should.Throw<BusinessRuleViolationException>(act);
        ex.Error.Code.ShouldBe("ticketed_event_reconfirm_policy.cadence_below_minimum");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TicketedEvent NewEvent() => TicketedEvent.Create(
        CreationRequestId.From(Guid.NewGuid()),
        DefaultEventId, DefaultTeamId, DefaultName, DefaultWebsite, DefaultBaseUrl, DefaultStart, DefaultEnd,
                TimeZoneId.From("UTC"));

    private static TicketedEventRegistrationPolicy NewRegistrationPolicy()
    {
        var now = DateTimeOffset.UtcNow;
        return TicketedEventRegistrationPolicy.Create(now.AddDays(-1), now.AddDays(30));
    }

    private static TicketedEventReconfirmPolicy NewReconfirmPolicy()
    {
        var now = DateTimeOffset.UtcNow;
        return TicketedEventReconfirmPolicy.Create(
            now.AddDays(-10), now.AddDays(-1), TimeSpan.FromDays(2), TimeSpan.FromHours(24));
    }
}
