using System.Text;
using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Humanizer;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeExport.ExportBadgeCsv;

internal sealed class ExportBadgeCsvHandler(
    IBadgesWriteStore writeStore,
    IRegistrationsFacade registrationsFacade)
    : IQueryHandler<ExportBadgeCsvQuery, (string FileName, byte[] Content)>
{
    public async ValueTask<(string FileName, byte[] Content)> HandleAsync(
        ExportBadgeCsvQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(query.EventId);
        var teamId = TeamId.From(query.TeamId);

        await writeStore.BadgesEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        var badgeTypeId = BadgeTypeId.From(query.BadgeTypeId);

        var badgeType = await writeStore.BadgeTypes.GetUntrackedAsync(
            bt => bt.Id == badgeTypeId && bt.EventId == eventId,
            cancellationToken);

        var csv = badgeType.Kind == BadgeKind.Standalone
            ? await BuildStandaloneCsvAsync(badgeTypeId, cancellationToken)
            : await BuildTicketBasedCsvAsync(eventId, badgeType, cancellationToken);

        var fileName = $"badges-{badgeType.Name.Value.Kebaberize()}.csv";
        return (fileName, Encoding.UTF8.GetBytes(csv));
    }

    private async Task<string> BuildStandaloneCsvAsync(
        BadgeTypeId badgeTypeId,
        CancellationToken cancellationToken)
    {
        var instances = await writeStore.BadgeInstances
            .AsNoTracking()
            .Where(bi => bi.BadgeTypeId == badgeTypeId)
            .OrderBy(bi => bi.DisplayName)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("DisplayName,Notes");

        foreach (var instance in instances)
        {
            sb.AppendLine($"{CsvEscape(instance.DisplayName.Value)},{CsvEscape(instance.Notes.Value)}");
        }

        return sb.ToString();
    }

    private async Task<string> BuildTicketBasedCsvAsync(
        TicketedEventId eventId,
        BadgeType badgeType,
        CancellationToken cancellationToken)
    {
        var ticketTypeGuids = badgeType.TicketTypeIds
            .Select(id => id.Value)
            .ToList();

        var registrations = await registrationsFacade.GetRegistrationsAsync(
            eventId.Value,
            new QueryRegistrationsDto(
                RegistrationStatus: RegistrationStatus.Registered,
                TicketTypeIds: ticketTypeGuids),
            cancellationToken);

        var schemaFields = await registrationsFacade.GetAdditionalDetailSchemaAsync(
            eventId.Value,
            cancellationToken);

        var sb = new StringBuilder();

        // Header row
        var headerParts = new List<string> { "FirstName", "LastName", "Email" };
        headerParts.AddRange(schemaFields.Select(f => CsvEscape(f.Key)));
        sb.AppendLine(string.Join(",", headerParts));

        // Data rows
        foreach (var reg in registrations)
        {
            var parts = new List<string>
            {
                CsvEscape(reg.FirstName),
                CsvEscape(reg.LastName),
                CsvEscape(reg.Email)
            };

            foreach (var field in schemaFields)
            {
                var value = reg.AdditionalDetails.TryGetValue(field.Key, out var v) ? v : string.Empty;
                parts.Add(CsvEscape(value));
            }

            sb.AppendLine(string.Join(",", parts));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
