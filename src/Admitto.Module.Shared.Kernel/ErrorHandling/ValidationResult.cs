namespace Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

public readonly struct ValidationResult
{
    private readonly Error? _error;

    private ValidationResult(Error? error) => _error = error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => _error is not null;

    public Error Error =>
        _error ?? throw new InvalidOperationException("Result is successful.");

    public static ValidationResult Success() => new(null);

    public static ValidationResult Failure(Error error) =>
        new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator ValidationResult(Error error) => Failure(error);

    public static ValidationResult<T> Ensure<T>(T? value, Error error)
        where T : struct =>
        value.HasValue
            ? ValidationResult<T>.Success(value.Value)
            : ValidationResult<T>.Failure(error);

    public static ValidationResult<T> Ensure<T>(T? value, Error error)
        where T : class =>
        value is not null
            ? ValidationResult<T>.Success(value)
            : ValidationResult<T>.Failure(error);
    
    public ValidationResult<TOut> Map<TOut>(Func<TOut> map) =>
        IsSuccess
            ? ValidationResult<TOut>.Success(map())
            : ValidationResult<TOut>.Failure(_error!);

}

public readonly struct ValidationResult<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private ValidationResult(T value)
    {
        _value = value;
        _error = null;
    }

    private ValidationResult(Error error)
    {
        _value = default;
        _error = error;
    }

    public bool IsSuccess => _error is null;
    public bool IsFailure => _error is not null;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Result is a failure.");

    public Error Error =>
        _error ?? throw new InvalidOperationException("Result is successful.");

    public static ValidationResult<T> Success(T value) => new(value);

    public static ValidationResult<T> Failure(Error error) =>
        new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator ValidationResult<T>(T value) => Success(value);
    public static implicit operator ValidationResult<T>(Error error) => Failure(error);

    public T GetValueOrThrow() =>
        IsSuccess
            ? _value!
            : throw new BusinessRuleViolationException(_error!);

    public ValidationResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess
            ? ValidationResult<TOut>.Success(map(_value!))
            : ValidationResult<TOut>.Failure(_error!);
    
    public ValidationResult<TOut> Then<TOut>(Func<T, ValidationResult<TOut>> next) =>
        IsSuccess
            ? next(Value)
            : ValidationResult<TOut>.Failure(Error);
    
    public async Task<ValidationResult<TOut>> ThenAsync<TOut>(Func<T, Task<ValidationResult<TOut>>> next) =>
        IsSuccess
            ? await next(Value)
            : ValidationResult<TOut>.Failure(Error);
    
    public Task<ValidationResult<TOut>> ThenAsync2<TOut>(Func<T, ValidationResult<TOut>> next) =>
        Task.FromResult(IsSuccess
            ? next(Value)
            : ValidationResult<TOut>.Failure(Error));
}
