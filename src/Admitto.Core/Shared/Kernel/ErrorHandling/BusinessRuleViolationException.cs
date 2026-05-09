namespace Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

public class BusinessRuleViolationException(Error error) : Exception
{
    public Error Error { get; } = error;
}