using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Email.Application;

/// <summary>
/// Builds <see cref="EventEmailContextView"/> projection rows, the Email-owned
/// scheduling oracle for reconfirm triggers. Defaults produce a row with an
/// active reconfirm policy whose window is open around <see cref="At"/>.
/// </summary>
public sealed class EventEmailContextViewBuilder
{
    private TeamId _teamId = TeamId.New();
    private TicketedEventId _eventId = TicketedEventId.New();
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    private string _eventName = "Test event";
    private string _websiteUrl = "https://example.test";
    private string _publicSlug = "test-event";
    private string? _timeZone = "UTC";
    private int _selfServiceTicketTypeCount = 1;
    private bool _isArchived;
    private bool _hasPolicy = true;
    private DateTimeOffset? _opensAt;
    private DateTimeOffset? _closesAt;
    private int _cadenceHours = 24;
    private int _minEmailIntervalHours = 24;

    public EventEmailContextViewBuilder ForTeam(TeamId teamId) { _teamId = teamId; return this; }

    public EventEmailContextViewBuilder ForEvent(TicketedEventId eventId) { _eventId = eventId; return this; }

    public EventEmailContextViewBuilder At(DateTimeOffset now) { _now = now; return this; }

    public EventEmailContextViewBuilder WithTimeZone(string timeZone) { _timeZone = timeZone; return this; }

    public EventEmailContextViewBuilder WithCadenceHours(int cadenceHours)
    {
        _cadenceHours = cadenceHours;
        return this;
    }

    public EventEmailContextViewBuilder WithMinEmailIntervalHours(int minEmailIntervalHours)
    {
        _minEmailIntervalHours = minEmailIntervalHours;
        return this;
    }

    /// <summary>Sets an explicit reconfirm window instead of the default open-around-now window.</summary>
    public EventEmailContextViewBuilder WithWindow(DateTimeOffset opensAt, DateTimeOffset closesAt)
    {
        _opensAt = opensAt;
        _closesAt = closesAt;
        return this;
    }

    /// <summary>Places the reconfirm window entirely in the future, so <see cref="At"/> falls before it opens.</summary>
    public EventEmailContextViewBuilder WithWindowNotYetOpen() =>
        WithWindow(_now.AddHours(1), _now.AddHours(2));

    /// <summary>Places the reconfirm window entirely in the past, so <see cref="At"/> falls after it closes.</summary>
    public EventEmailContextViewBuilder WithWindowAlreadyClosed() =>
        WithWindow(_now.AddHours(-2), _now.AddHours(-1));

    public EventEmailContextViewBuilder WithoutReconfirmPolicy() { _hasPolicy = false; return this; }

    /// <summary>
    /// Leaves the row in the partial state it has before the details-changed
    /// event lands, so no time zone is projected yet.
    /// </summary>
    public EventEmailContextViewBuilder WithoutEventContext() { _timeZone = null; return this; }

    public EventEmailContextViewBuilder Archived() { _isArchived = true; return this; }

    public EventEmailContextView Build()
    {
        var view = EventEmailContextView.CreatePartial(_teamId, _eventId, _now);

        // A row with no projected time zone never received the event-context
        // update, so leave it partial rather than writing a null time zone.
        if (_timeZone is null)
            return view;

        var policy = _hasPolicy
            ? new TicketedEventReconfirmPolicySnapshot(
                _opensAt ?? _now.AddHours(-1),
                _closesAt ?? _now.AddHours(1),
                _cadenceHours,
                _minEmailIntervalHours)
            : null;

        view.UpdateEventContext(
            ticketedEventVersion: 1,
            eventName: _eventName,
            websiteUrl: _websiteUrl,
            publicSlug: _publicSlug,
            timeZone: _timeZone,
            selfServiceTicketTypeCount: _selfServiceTicketTypeCount,
            reconfirmPolicy: policy,
            isArchived: _isArchived,
            now: _now);

        return view;
    }
}
