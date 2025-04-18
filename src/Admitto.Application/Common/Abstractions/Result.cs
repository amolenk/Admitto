namespace Amolenk.Admitto.Application.Common.Abstractions;

public class Result<T>
{
    public T Value { get; }
    public bool IsSuccess { get; }
    public string Error { get; }

    protected Result(T value, bool isSuccess, string error)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value, true, string.Empty);
    public static Result<T> Failure(string error) => new(default!, false, error);
    public static Result<T> NotFound() => new(default!, false, "Not Found");
}
