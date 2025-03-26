namespace Amolenk.Admitto.Application.UseCases.Email;

public interface IEmailContext
{
    DbSet<EmailMessage> EmailMessages { get; }
}