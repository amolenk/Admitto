using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailContext
{
    DbSet<EmailMessage> EmailMessages { get; }
}