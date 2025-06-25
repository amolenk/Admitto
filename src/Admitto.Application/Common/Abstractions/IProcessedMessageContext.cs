using Amolenk.Admitto.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Application.Common.Abstractions;

/// <summary>
/// Provides access to processed messages for exactly-once processing.
/// </summary>
public interface IProcessedMessageContext
{
    DbSet<ProcessedMessage> ProcessedMessages { get; }
}