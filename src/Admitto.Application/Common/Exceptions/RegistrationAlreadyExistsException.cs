namespace Amolenk.Admitto.Application.Common.Exceptions;

public class RegistrationAlreadyExistsException(Exception? innerException = null)
    : Exception("Registration already exists.", innerException);