using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeExport.ExportBadgeCsv;

internal sealed record ExportBadgeCsvQuery(Guid EventId, Guid TeamId, Guid BadgeTypeId)
    : Query<(string FileName, byte[] Content)>;
