namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface ICommand
{
    Guid CommandId { get; }
}