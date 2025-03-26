namespace Amolenk.Admitto.Application.UseCases.Accounts.GetUserToken;

public record GetUserTokenCommand(Guid Code, string CodeVerifier) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}

public record GetUserTokenResult(string AccessToken, string RefreshToken);