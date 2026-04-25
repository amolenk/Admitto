using System.Text.Json.Serialization;
using Amolenk.Admitto.Module.Registrations.Contracts;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// Discriminated value object describing how a <see cref="Entities.BulkEmailJob"/>
/// resolves its recipients. Per the bulk-email spec there are exactly two shapes:
/// <see cref="AttendeeSource"/> and <see cref="ExternalListSource"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AttendeeSource), typeDiscriminator: "attendee")]
[JsonDerivedType(typeof(ExternalListSource), typeDiscriminator: "external_list")]
public abstract record BulkEmailJobSource;

/// <summary>
/// Resolves recipients via <c>IRegistrationsFacade.QueryRegistrationsAsync</c>.
/// </summary>
public sealed record AttendeeSource(QueryRegistrationsDto Filter) : BulkEmailJobSource;

/// <summary>
/// A literal recipient list supplied at request time.
/// </summary>
public sealed record ExternalListSource(IReadOnlyList<ExternalListItem> Items) : BulkEmailJobSource;
