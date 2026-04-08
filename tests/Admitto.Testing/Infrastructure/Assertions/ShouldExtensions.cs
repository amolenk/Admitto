using System.Collections;
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
            var actualValue = actual.Details[expectedDetail.Key];
            var expectedValue = expectedDetail.Value;

            // Compare collections element-wise to avoid type mismatches between
            // different IEnumerable implementations (e.g. List vs ReadOnlySingleElementList).
            if (actualValue is IEnumerable actualEnumerable and not string
                && expectedValue is IEnumerable expectedEnumerable and not string)
            {
                actualEnumerable.Cast<object>().ToList()
                    .ShouldBeEquivalentTo(expectedEnumerable.Cast<object>().ToList());
            }
            else
            {
                actualValue.ShouldBeEquivalentTo(expectedValue);
            }
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