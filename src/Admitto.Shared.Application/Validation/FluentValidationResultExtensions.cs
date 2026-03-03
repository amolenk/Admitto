using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;

namespace Amolenk.Admitto.Shared.Application.Validation;

public static class FluentValidationResultExtensions
{
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
}