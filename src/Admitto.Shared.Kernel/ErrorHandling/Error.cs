namespace Amolenk.Admitto.Shared.Kernel.ErrorHandling;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
}

public sealed record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Validation,
    IReadOnlyDictionary<string, object?>? Details = null);
    