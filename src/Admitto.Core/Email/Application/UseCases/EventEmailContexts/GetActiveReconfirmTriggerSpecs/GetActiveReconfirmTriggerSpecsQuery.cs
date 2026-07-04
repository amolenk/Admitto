using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetActiveReconfirmTriggerSpecs;

/// <summary>
/// Returns the <see cref="ReconfirmTriggerSpecDto"/> for every event whose
/// Email-owned projection currently carries an active reconfirm schedule
/// context. Used by reconciliation to rebuild Quartz triggers from projection
/// state.
/// </summary>
internal sealed record GetActiveReconfirmTriggerSpecsQuery
    : Query<IReadOnlyList<ReconfirmTriggerSpecDto>>;
