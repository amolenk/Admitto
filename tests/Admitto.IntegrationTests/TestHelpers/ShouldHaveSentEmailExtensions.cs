namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

public static class ShouldHaveSentEmailExtensions
{
    public static async ValueTask ShouldHaveSentSingleEmailAsync(this EmailTestFixture fixture, 
        params Action<SentEmail>[] conditions)
    {
        var emails = (await fixture.GetSentEmailsAsync()).ToList();
        
        emails.ShouldNotBeNull();
        emails.Count.ShouldBe(1);
        emails[0].ShouldSatisfyAllConditions(conditions);
    }
    
    public static async ValueTask ShouldNotHaveSentEmailAsync(this EmailTestFixture fixture)
    {
        var emails = (await fixture.GetSentEmailsAsync()).ToList();
        
        emails.ShouldBeEmpty();
    }
}
