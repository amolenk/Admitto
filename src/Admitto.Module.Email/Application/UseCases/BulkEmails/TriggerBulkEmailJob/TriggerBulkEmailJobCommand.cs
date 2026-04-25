using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob;

/// <summary>
/// Schedules a one-shot Quartz trigger that drives the fan-out for a bulk email job.
/// </summary>
internal sealed record TriggerBulkEmailJobCommand(BulkEmailJobId BulkEmailJobId) : Command;
