using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Application.Tests;

public static class AssertionExtensions
{
    [Obsolete("Use ShouldHaveProblemDetail instead.")]
    public static Task ShouldHaveProblemDetail(this HttpResponseMessage response, string errorKey)
    {
        return response.ShouldHaveProblemDetail(pd => pd.Errors.ShouldContainKey(errorKey));
    }
    
    public static async Task ShouldHaveProblemDetail(this HttpResponseMessage response, 
        params Action<ValidationProblemDetails>[] assertProblemDetails)
    {
        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        validationProblem.ShouldNotBeNull();

        foreach (var assertion in assertProblemDetails)
        {
            assertion(validationProblem);
        }
    }
}