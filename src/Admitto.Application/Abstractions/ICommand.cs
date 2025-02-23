namespace Amolenk.Admitto.Application.Abstractions;

public interface ICommand : IRequest
{
    Guid CommandId { get; }
}