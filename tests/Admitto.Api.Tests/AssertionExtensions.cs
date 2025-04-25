using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Application.Tests;

public static class AssertionExtensions
{
    public static async Task ShouldHaveProblemDetail(this HttpResponseMessage response, string errorKey)
    {
        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        validationProblem.ShouldNotBeNull();
        validationProblem.Errors.ShouldContainKey(errorKey);
    }
}