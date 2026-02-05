namespace Amolenk.Admitto.Shared.Kernel.ErrorHandling;

public static class SharedErrors
{
    public static class ValueObjects
    {
        public static Error Required(string name) =>
            new($"{name}.required", $"{name} is required.");

        public static Error Empty(string name) =>
            new($"{name}.empty", $"{name} cannot be empty.");

        public static Error TooLong(string name, int max) =>
            new($"{name}.too_long", $"{name} exceeds max length {max}.");

        public static Error InvalidFormat(string name) =>
            new($"{name}.invalid_format", $"{name} has an invalid format.");
    }
}