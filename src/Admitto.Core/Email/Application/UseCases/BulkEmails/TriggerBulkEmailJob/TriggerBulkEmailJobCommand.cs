using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob;

/// <summary>
/// Schedules a one-shot Quartz trigger that drives the fan-out for a bulk email job.
/// </summary>
internal sealed record TriggerBulkEmailJobCommand(Guid BulkEmailJobId) : Command;
