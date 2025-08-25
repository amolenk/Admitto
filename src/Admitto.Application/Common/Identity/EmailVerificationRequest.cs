using System.Security.Cryptography;
using Amolenk.Admitto.Application.Common.Cryptography;

namespace Amolenk.Admitto.Application.Common.Identity;

public class EmailVerificationRequest
{
    private EmailVerificationRequest()
    {
    }

    private EmailVerificationRequest(
        Guid id,
        Guid ticketedEventId,
        string email,
        string hashedCode,
        DateTimeOffset requestedAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        TicketedEventId = ticketedEventId;
        Email = email;
        HashedCode = hashedCode;
        RequestedAt = requestedAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public string HashedCode { get; private set; } = null!;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public static EmailVerificationRequest Create(
        Guid ticketedEventId,
        string email,
        ISigningService signingService,
        out string code)
    {
        code = GenerateCode();
        var hashedCode = signingService.Sign(code);

        return new EmailVerificationRequest(
            Guid.NewGuid(),
            ticketedEventId,
            email,
            hashedCode,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(15));
    }

    public bool Verify(string code, ISigningService signingService)
    {
        return signingService.IsValid(code, HashedCode);
    }

    private static string GenerateCode()
    {
        // Generates a random 6-digit code (000000-999999)
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);

        var value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Ensure non-negative
        var code = value % 1_000_000;

        return code.ToString("D6");
    }
}