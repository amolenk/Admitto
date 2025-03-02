namespace Amolenk.Admitto.Application.Exceptions;

public class RegistrationAlreadyExistsException(Exception? innerException = null)
    : Exception("Registration already exists.", innerException);