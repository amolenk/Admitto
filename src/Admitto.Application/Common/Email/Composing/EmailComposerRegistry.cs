namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Classes that implement this interface can provide email composers for different email types.
/// </summary>
public interface IEmailComposerRegistry
{
    IEmailComposer GetEmailComposer(string emailType);
}

/// <summary>
/// Default implementation of <see cref="IEmailComposerRegistry"/> that provides email composers based on keyed
/// services registered in the service provider.
/// </summary>
public class EmailComposerRegistry(IServiceProvider serviceProvider) : IEmailComposerRegistry
{
    public IEmailComposer GetEmailComposer(string emailType)
    {
        var composer = serviceProvider.GetKeyedService<IEmailComposer>(emailType);
        if (composer is null)
        {
            throw new ArgumentException("No email composer found for the specified email type.", nameof(emailType));
        }

        return composer;
    }
}

