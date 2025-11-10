namespace Amolenk.Admitto.Application.UseCases.EmailRecipientLists.AddEmailRecipientList;

public record AddEmailRecipientListRequest(string Name, EmailRecipientDto[] Recipients);

public record EmailRecipientDto(string Email, EmailRecipientDetailDto[] Details);

public record EmailRecipientDetailDto(string Name, string Value);