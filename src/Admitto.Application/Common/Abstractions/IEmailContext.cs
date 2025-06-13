using Amolenk.Admitto.Application.UseCases.Email;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailContext
{
    DbSet<EmailMessage> EmailMessages { get; }
}