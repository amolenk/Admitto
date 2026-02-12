using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence.ValueConverters;

internal sealed class SlugConverter() : ValueConverter<Slug, string>(
    v => v.Value,
    v => Slug.From(v));
