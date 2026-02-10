namespace Amolenk.Admitto.Shared.Application.Mapping;

public static class SharedMapping
{
    /// <summary>
    /// Applies the changes from a list of domain items to a list of records.
    /// It adds new records, updates existing ones, and removes records that are no longer present.
    /// </summary>
    public static void ApplyToRecords<TDomain, TRecord, TKey>(
        this IReadOnlyCollection<TDomain> domainObjects,
        IList<TRecord> records,
        Func<TDomain, TKey> domainKeySelector,
        Func<TRecord, TKey> recordKeySelector,
        Func<TDomain, TRecord> mapDomainToNewRecord,
        Action<TDomain, TRecord> applyDomainToExistingRecord)
        where TKey : notnull
    {
        // Tickets: diff by TicketTypeId
        var desiredByKey = domainObjects.ToDictionary(domainKeySelector);
        var existingById = records.ToDictionary(recordKeySelector);

        // Remove
        for (var i = records.Count - 1; i >= 0; i--)
        {
            var existing = records[i];
            if (!desiredByKey.ContainsKey(recordKeySelector(existing)))
            {
                records.RemoveAt(i);
            }
        }

        // Add / Update
        foreach (var (key, desired) in desiredByKey)
        {
            if (!existingById.TryGetValue(key, out var existing))
            {
                records.Add(mapDomainToNewRecord(desired));
            }
            else
            {
                applyDomainToExistingRecord(desired, existing);
            }
        }
    }
}