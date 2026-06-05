using System.Text;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Humanizer;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ExportRegistrationsCsv;

internal sealed class ExportRegistrationsCsvHandler(
    IQueryHandler<GetRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>?> getRegistrationsHandler,
    IRegistrationsWriteStore writeStore)
    : IQueryHandler<ExportRegistrationsCsvQuery, (string FileName, byte[] Content)?>
{
    public async ValueTask<(string FileName, byte[] Content)?> HandleAsync(
        ExportRegistrationsCsvQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(query.EventId);
        var teamId = TeamId.From(query.TeamId);

        var registrations = await getRegistrationsHandler.HandleAsync(
            new GetRegistrationsQuery(eventId, teamId),
            cancellationToken);

        if (registrations is null)
            return null;

        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ticketedEvent is null)
            return null;

        var schemaFields = ticketedEvent.AdditionalDetailSchema.Fields;

        var csv = BuildCsv(registrations, schemaFields);

        var fileName = $"registrations-{ticketedEvent.Name.Value.Kebaberize()}.csv";
        return (fileName, Encoding.UTF8.GetBytes(csv));
    }

    private static string BuildCsv(
        IReadOnlyList<RegistrationListItemDto> registrations,
        IReadOnlyList<AdditionalDetailField> schemaFields)
    {
        var sb = new StringBuilder();

        var headerParts = new List<string> { "FirstName", "LastName", "Email", "Tickets", "Status", "RegisteredAt" };
        headerParts.AddRange(schemaFields.Select(f => CsvEscape(f.Name)));
        sb.AppendLine(string.Join(",", headerParts));

        foreach (var reg in registrations)
        {
            var parts = new List<string>
            {
                CsvEscape(reg.FirstName),
                CsvEscape(reg.LastName),
                CsvEscape(reg.Email),
                CsvEscape(string.Join("; ", reg.Tickets.Select(t => t.Name))),
                CsvEscape(reg.Status.ToString()),
                CsvEscape(reg.CreatedAt.ToString("o"))
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
