using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Vogen;

namespace Amolenk.Admitto.Core.Shared.Application.Validation;

public static class FluentValidationResultExtensions
{
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
