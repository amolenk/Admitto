using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

public static class ShouldHaveProblemDetailExtension
{
    public static async Task ShouldHaveProblemDetailAsync(this HttpResponseMessage response,
        HttpStatusCode? expectedStatusCode = null, params Action<ValidationProblemDetails>[] conditions)
    {
        response.StatusCode.ShouldBe(expectedStatusCode ?? HttpStatusCode.BadRequest);
        
        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        validationProblem.ShouldNotBeNull();

        foreach (var condition in conditions)
        {
            condition(validationProblem);
        }
    }
}