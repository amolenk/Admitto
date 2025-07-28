namespace Amolenk.Admitto.Domain.Entities;

public interface IHasConcurrencyToken
{
    uint Version { get; set; }
}