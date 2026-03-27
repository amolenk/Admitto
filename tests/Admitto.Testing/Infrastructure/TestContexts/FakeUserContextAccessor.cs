using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Contracts;

namespace Amolenk.Admitto.Testing.Infrastructure.TestContexts;

public class FakeUserContextAccessor : IUserContextAccessor
{
    public static readonly Guid DefaultUserId = Guid.NewGuid();
    public const string DefaultUserName = "Test User";
    public const string DefaultEmailAddress = "test.user@example.com";
    
    public UserContextDto Current => new (DefaultUserId, DefaultUserName, DefaultEmailAddress);
}