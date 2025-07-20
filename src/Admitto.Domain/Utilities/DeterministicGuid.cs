using System.Security.Cryptography;
using System.Text;


namespace Amolenk.Admitto.Domain.Utilities;

public static class DeterministicGuid
{
    // Random GUID for the Admitto namespace, used to create deterministic GUIDs.
    private static readonly Guid AdmittoNamespace = Guid.Parse("70514f52-c59f-4467-b054-b61dac3cbba3");
    
    public static Guid Create(string input) => Create(input, AdmittoNamespace);
    
    public static Guid Create(string input, Guid namespaceId)
    {
        var nameBytes = Encoding.UTF8.GetBytes(input);
        var nsBytes = namespaceId.ToByteArray();

        SwapByteOrder(nsBytes);
        var data = new byte[nsBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(nsBytes, 0, data, 0, nsBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, data, nsBytes.Length, nameBytes.Length);
        var hash = SHA1.HashData(data);
        var newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        newGuid[6] = (byte)((newGuid[6] & 0x0F) | 0x50); // UUIDv5
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80); // Variant RFC4122

        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        Swap(0, 3); Swap(1, 2); Swap(4, 5); Swap(6, 7);
        return;
        void Swap(int a, int b) => (guid[a], guid[b]) = (guid[b], guid[a]);
    }
}
