namespace Amolenk.Admitto.Application.Exceptions;

public class ConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);