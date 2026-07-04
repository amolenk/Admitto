using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ExportRegistrationsCsv;

/// <summary>
/// Builds a CSV export of every registration on the given ticketed event.
/// Returns <c>null</c> when the event does not belong to the supplied team
/// (translated to a 404 by the admin HTTP endpoint).
/// </summary>
internal sealed record ExportRegistrationsCsvQuery(Guid EventId, Guid TeamId)
    : Query<(string FileName, byte[] Content)?>;
