namespace Amolenk.Admitto.Shared.Kernel.ErrorHandling;

public class BusinessRuleViolationException(Error error) : Exception
{
    public Error Error { get; } = error;
}