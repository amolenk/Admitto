namespace Amolenk.Admitto.Application.Common.Exceptions;

public class DomainObjectNotFoundException(string message, Exception? innerException = null)
    : Exception(message, innerException);