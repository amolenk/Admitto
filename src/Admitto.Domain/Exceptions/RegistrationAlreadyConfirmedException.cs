namespace Amolenk.Admitto.Domain.Exceptions;

public class RegistrationAlreadyConfirmedException : DomainException
{
    public RegistrationAlreadyConfirmedException()
    : base("The registration has already been confirmed.")
    {
        throw new NotImplementedException();
    }
}