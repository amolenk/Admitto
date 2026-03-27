using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;

public class UserBuilder
{
    public static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");
    
    private EmailAddress _emailAddress = DefaultEmail;
    
    public UserBuilder WithEmailAddress(EmailAddress emailAddress)
    {
        _emailAddress = emailAddress;
        return this;
    }

    public User Build()
    {
        return User.Create(_emailAddress);
    }
}