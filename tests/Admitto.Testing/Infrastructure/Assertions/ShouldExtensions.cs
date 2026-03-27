using System.Text.Json;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Amolenk.Admitto.Testing.Infrastructure.Assertions;

public static class ShouldExtensions
{
    public static void ShouldMatch(this Error? actual, Error expected)
    {
        actual.ShouldNotBeNull();
        actual.Code.ShouldBe(expected.Code);
        actual.Type.ShouldBe(expected.Type);
        actual.Message.ShouldBe(expected.Message);

        if (actual.Details is null && expected.Details is null)
            return;
        
        if (actual.Details is null || expected.Details is null)
            throw new ShouldAssertException("Cannot assert details because one of the errors has no details.");
        
        actual.Details.Count.ShouldBe(expected.Details.Count);

        foreach (var expectedDetail in expected.Details)
        {
            actual.Details[expectedDetail.Key].ShouldBeEquivalentTo(expectedDetail.Value);
        }
    }
    
    public static ProblemDetails ShouldBeProblemDetails(this HttpContent content)
    {
        using var stream = content.ReadAsStream();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(stream);
        return problemDetails ?? throw new ShouldAssertException("Problem details not found");
    }

    public static void ShouldHaveErrorCode(this ProblemDetails problemDetails, string expectedCode)
    {
        const string codeKey = "code";
        problemDetails.Extensions.ShouldContainKey(codeKey);
        problemDetails.Extensions[codeKey].ShouldBeOfType<JsonElement>().GetString().ShouldBe(expectedCode);
    }
}