namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

public record SendEmailCommand(Guid EmailId) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}
