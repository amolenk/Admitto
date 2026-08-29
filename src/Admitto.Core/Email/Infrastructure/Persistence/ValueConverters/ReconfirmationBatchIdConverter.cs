using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.ValueConverters;

internal sealed class ReconfirmationBatchIdConverter()
    : ValueConverter<ReconfirmationBatchId, Guid>(
        id => id.Value,
        value => ReconfirmationBatchId.From(value));
