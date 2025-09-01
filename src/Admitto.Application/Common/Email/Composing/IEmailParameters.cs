using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Classes that implement this interface represent template parameters for emails.
/// </summary>
public interface IEmailParameters
{
    string Recipient { get; }
    
    EmailRecipientType RecipientType { get; }
}