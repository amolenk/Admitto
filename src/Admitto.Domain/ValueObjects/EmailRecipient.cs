namespace Amolenk.Admitto.Domain.ValueObjects;

public class EmailRecipient
{
    public string Email { get; set; } = null!;

    public List<EmailRecipientDetail> Details { get; set; } = [];
}
