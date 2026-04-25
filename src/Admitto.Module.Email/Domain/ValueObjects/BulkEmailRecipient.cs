namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// Frozen, per-recipient snapshot persisted on a
/// <see cref="Entities.BulkEmailJob"/> when it transitions from
/// <see cref="BulkEmailJobStatus.Resolving"/> to
/// <see cref="BulkEmailJobStatus.Sending"/>. The fan-out worker iterates this
/// snapshot and updates each entry's <see cref="Status"/> as it processes.
/// </summary>
public sealed class BulkEmailRecipient
{
    private BulkEmailRecipient()
    {
        Email = default!;
        ParametersJson = "{}";
    }

    public BulkEmailRecipient(
        string email,
        string? displayName,
        Guid? registrationId,
        string parametersJson)
    {
        Email = email;
        DisplayName = displayName;
        RegistrationId = registrationId;
        ParametersJson = string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson;
        Status = BulkEmailRecipientStatus.Pending;
        LastError = null;
    }

    public string Email { get; private set; }
    public string? DisplayName { get; private set; }
    public Guid? RegistrationId { get; private set; }
    public string ParametersJson { get; private set; }
    public BulkEmailRecipientStatus Status { get; private set; }
    public string? LastError { get; private set; }

    internal void MarkSent()
    {
        Status = BulkEmailRecipientStatus.Sent;
        LastError = null;
    }

    internal void MarkFailed(string error)
    {
        Status = BulkEmailRecipientStatus.Failed;
        LastError = error;
    }

    internal void MarkCancelled()
    {
        Status = BulkEmailRecipientStatus.Cancelled;
    }
}
