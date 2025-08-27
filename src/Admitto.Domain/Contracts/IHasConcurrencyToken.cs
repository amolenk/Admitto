namespace Amolenk.Admitto.Domain.Contracts;

public interface IHasConcurrencyToken
{
    uint Version { get; set; }
}