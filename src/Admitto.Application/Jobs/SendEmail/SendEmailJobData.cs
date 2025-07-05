using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Jobs.SendEmail;

public record SendEmailJobData(Guid Id, Guid RegistrationId, EmailTemplateId TemplateId) : IJobData;