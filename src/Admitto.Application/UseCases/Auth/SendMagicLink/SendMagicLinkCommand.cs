namespace Amolenk.Admitto.Application.UseCases.Auth.SendMagicLink;

public record SendMagicLinkCommand(string Email, string CodeChallenge) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}
