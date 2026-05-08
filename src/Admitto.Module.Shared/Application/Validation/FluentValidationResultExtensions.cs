using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Vogen;

namespace Amolenk.Admitto.Module.Shared.Application.Validation;

public static class FluentValidationResultExtensions
{
    // Legacy overloads for hand-crafted VOs (ValidationResult<TOut>)

    public static IRuleBuilderOptionsConditions<T, TProperty> MustBeNullOrParseable<T, TProperty, TOut>(
        this IRuleBuilderInitial<T, TProperty> ruleBuilder,
        Func<TProperty, ValidationResult<TOut>> func)
        => ruleBuilder.Custom((value, context) =>
            {
                if (value is not null)
                {
                    AddParseFailure(value, context, func);
                }
            });

    public static IRuleBuilderOptionsConditions<T, TProperty> MustBeParseable<T, TProperty, TOut>(
        this IRuleBuilderInitial<T, TProperty> ruleBuilder,
        Func<TProperty, ValidationResult<TOut>> func)
        => ruleBuilder.Custom((value, context) => AddParseFailure(value, context, func));

    public static IRuleBuilderOptionsConditions<T, TElement> MustBeParseable<T, TElement, TOut>(
        this IRuleBuilderInitialCollection<T, TElement> ruleBuilder,
        Func<TElement, ValidationResult<TOut>> func)
        => ruleBuilder.Custom((value, context) => AddParseFailure(value, context, func));

    private static void AddParseFailure<T, TValue, TOut>(
        TValue value,
        ValidationContext<T> context,
        Func<TValue, ValidationResult<TOut>> func)
    {
        var result = func(value);
        if (result.IsSuccess) return;

        var error = result.Error;

        context.AddFailure(new ValidationFailure(context.PropertyPath, error.Message)
        {
            ErrorCode = error.Code
        });
    }

    // Vogen overloads (ValueObjectOrError<TOut>)

    public static IRuleBuilderOptionsConditions<T, TProperty> MustBeNullOrParseable<T, TProperty, TOut>(
        this IRuleBuilderInitial<T, TProperty> ruleBuilder,
        Func<TProperty, ValueObjectOrError<TOut>> func)
        => ruleBuilder.Custom((value, context) =>
            {
                if (value is not null)
                {
                    AddParseFailure(value, context, func);
                }
            });

    /// <summary>
    /// Vogen-specific overload for nullable string properties where TryFrom takes a non-nullable string.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, string?> MustBeNullOrParseable<T, TOut>(
        this IRuleBuilderInitial<T, string?> ruleBuilder,
        Func<string, ValueObjectOrError<TOut>> func)
        => ruleBuilder.Custom((value, context) =>
            {
                if (value is not null)
                {
                    AddParseFailure(value, context, func);
                }
            });

    public static IRuleBuilderOptionsConditions<T, TProperty> MustBeParseable<T, TProperty, TOut>(
        this IRuleBuilderInitial<T, TProperty> ruleBuilder,
        Func<TProperty, ValueObjectOrError<TOut>> func)
        => ruleBuilder.Custom((value, context) => AddParseFailure(value, context, func));

    public static IRuleBuilderOptionsConditions<T, TElement> MustBeParseable<T, TElement, TOut>(
        this IRuleBuilderInitialCollection<T, TElement> ruleBuilder,
        Func<TElement, ValueObjectOrError<TOut>> func)
        => ruleBuilder.Custom((value, context) => AddParseFailure(value, context, func));

    private static void AddParseFailure<T, TValue, TOut>(
        TValue value,
        ValidationContext<T> context,
        Func<TValue, ValueObjectOrError<TOut>> func)
    {
        var result = func(value);
        if (result.IsSuccess) return;

        context.AddFailure(new ValidationFailure(context.PropertyPath, result.Error.ErrorMessage));
    }
}