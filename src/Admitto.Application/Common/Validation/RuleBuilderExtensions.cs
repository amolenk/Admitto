namespace Amolenk.Admitto.Application.Common.Validation;

public static class RuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, string> Slug<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$");
    }
}