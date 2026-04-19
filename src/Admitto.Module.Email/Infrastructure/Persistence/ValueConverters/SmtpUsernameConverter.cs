using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.ValueConverters;

internal sealed class SmtpUsernameConverter() : ValueConverter<SmtpUsername, string>(
    v => v.Value,
    v => SmtpUsername.From(v));
