namespace Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

// TODO Split into separate classes, like NotFoundError, etc.
public class CommonErrors
{


    public static readonly Error TextEmpty = new(
      "text.empty",
      "Text is required.");

    public static Error TextTooLong(int maxLength) => new(
      "text.too_long",
      $"Text must be at most {maxLength} character(s).");
}
