namespace Amolenk.Admitto.Application.Abstractions;

public interface ICommand
{
    Guid CommandId { get; }
}