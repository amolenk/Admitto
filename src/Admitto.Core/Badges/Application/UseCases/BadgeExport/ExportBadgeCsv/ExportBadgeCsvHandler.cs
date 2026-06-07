using System.Globalization;
using System.Text;
using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using CsvHelper;
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

        // Load BadgeEvent (untracked)
        var badgeEvent = await writeStore.BadgeEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        var badgeTypeId = BadgeTypeId.From(query.BadgeTypeId);

        // Find the badge type in the event's collection
        var badgeType = badgeEvent.BadgeTypes.FirstOrDefault(bt => bt.Id == badgeTypeId);
        if (badgeType is null)
            throw new BusinessRuleViolationException(BadgeEvent.Errors.BadgeTypeNotFound);

        var content = badgeType.Kind == BadgeKind.Standalone
            ? await BuildStandaloneCsvAsync(teamId, eventId, badgeTypeId, cancellationToken)
            : await BuildTicketBasedCsvAsync(eventId, badgeType, cancellationToken);

        var fileName = $"badges-{badgeType.Name.Value.Kebaberize()}.csv";
        return (fileName, content);
    }

    private async Task<byte[]> BuildStandaloneCsvAsync(
        TeamId teamId,
        TicketedEventId eventId,
        BadgeTypeId badgeTypeId,
        CancellationToken cancellationToken)
    {
        var instances = await writeStore.BadgeInstances
            .AsNoTracking()
            .Where(bi => bi.TeamId == teamId && bi.EventId == eventId && bi.BadgeTypeId == badgeTypeId)
            .OrderBy(bi => bi.DisplayName)
            .ToListAsync(cancellationToken);

        using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await using var csv = new CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = "\n" });

        csv.WriteField("DisplayName");
        csv.WriteField("Notes");
        await csv.NextRecordAsync();

        foreach (var instance in instances)
        {
            csv.WriteField(instance.DisplayName.Value);
            csv.WriteField(instance.Notes.Value);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
        return ms.ToArray();
    }

    private async Task<byte[]> BuildTicketBasedCsvAsync(
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

        using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await using var csv = new CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = "\n" });

        // Header row
        csv.WriteField("FirstName");
        csv.WriteField("LastName");
        csv.WriteField("Email");
        foreach (var field in schemaFields)
        {
            csv.WriteField(field.Key);
        }
        await csv.NextRecordAsync();

        // Data rows
        foreach (var reg in registrations)
        {
            csv.WriteField(reg.FirstName);
            csv.WriteField(reg.LastName);
            csv.WriteField(reg.Email);
            foreach (var field in schemaFields)
            {
                var value = reg.AdditionalDetails.TryGetValue(field.Key, out var v) ? v : string.Empty;
                csv.WriteField(value);
            }
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
        return ms.ToArray();
    }
}
