namespace Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
}

public sealed record Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, object?>? Details = null,
    ErrorType Type = ErrorType.Validation);
    