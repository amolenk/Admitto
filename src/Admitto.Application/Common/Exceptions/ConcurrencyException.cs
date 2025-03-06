namespace Amolenk.Admitto.Application.Common.Exceptions;

public class ConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);