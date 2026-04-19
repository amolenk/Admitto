using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.ValueConverters;

internal sealed class HostnameConverter() : ValueConverter<Hostname, string>(
    v => v.Value,
    v => Hostname.From(v));
