using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Classes that implement this interface can provide email composers for different email types.
/// </summary>
public interface IEmailComposerRegistry
{
    IEmailComposer GetEmailComposer(EmailType emailType);
    
    IBulkEmailComposer GetBulkEmailComposer(EmailType emailType);
}

/// <summary>
/// Default implementation of <see cref="IEmailComposerRegistry"/> that provides email composers based on keyed
/// services registered in the service provider.
/// </summary>
public class EmailComposerRegistry(IServiceProvider serviceProvider) : IEmailComposerRegistry
{
    public IEmailComposer GetEmailComposer(EmailType emailType)
    {
        var composer = serviceProvider.GetKeyedService<IEmailComposer>(emailType);
        if (composer is null)
        {
            throw new ArgumentException("No email composer found for the specified email type.", nameof(emailType));
        }

        return composer;
    }
    
    public IBulkEmailComposer GetBulkEmailComposer(EmailType emailType)
    {
        var composer = serviceProvider.GetKeyedService<IBulkEmailComposer>(emailType);
        if (composer is null)
        {
            throw new ArgumentException("No bulk email composer found for the specified email type.", nameof(emailType));
        }

        return composer;
    }
}

