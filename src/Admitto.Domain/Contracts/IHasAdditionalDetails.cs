using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Contracts;

public interface IHasAdditionalDetails
{
    /// <summary>
    /// Additional details associated with the entity.
    /// </summary>
    IReadOnlyCollection<AdditionalDetail> AdditionalDetails { get; }
}