using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Lalamog.Api.Infrastructure;

public static class PasswordHashing
{
    private const int Iterations = 220_000;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, 32);
        // Version, algorithm, iterations and salt length precede salt and derived key.
        byte[] payload = new byte[13 + salt.Length + key.Length];
        payload[0] = 1;
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(1, 4), 2);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(5, 4), Iterations);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(9, 4), (uint)salt.Length);
        salt.CopyTo(payload, 13);
        key.CopyTo(payload, 13 + salt.Length);
        CryptographicOperations.ZeroMemory(key);
        return Convert.ToBase64String(payload);
    }

    public static bool Verify(string? storedHash, string password)
    {
        if (string.IsNullOrEmpty(storedHash) || storedHash.Length > 4096)
        {
            return false;
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (payload.Length == 49 && payload[0] == 0)
        {
            return VerifyKey(password, payload.AsSpan(1, 16), payload.AsSpan(17), 1000, HashAlgorithmName.SHA1);
        }
        if (payload.Length < 45 || payload[0] != 1)
        {
            return false;
        }

        uint algorithm = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(1, 4));
        uint iterations = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(5, 4));
        uint saltLength = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(9, 4));
        if (iterations < 1 || iterations > 10_000_000 || saltLength < 16
            || saltLength > 1024 || saltLength > payload.Length - 29)
        {
            return false;
        }

        HashAlgorithmName hashAlgorithm;
        switch (algorithm)
        {
            case 0: hashAlgorithm = HashAlgorithmName.SHA1; break;
            case 1: hashAlgorithm = HashAlgorithmName.SHA256; break;
            case 2: hashAlgorithm = HashAlgorithmName.SHA512; break;
            default: return false;
        }
        return VerifyKey(password, payload.AsSpan(13, (int)saltLength),
            payload.AsSpan(13 + (int)saltLength), (int)iterations, hashAlgorithm);
    }

    private static bool VerifyKey(string password, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> expected,
        int iterations, HashAlgorithmName algorithm)
    {
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password.AsSpan(), salt, iterations, algorithm, expected.Length);
        bool valid = CryptographicOperations.FixedTimeEquals(actual, expected);
        CryptographicOperations.ZeroMemory(actual);
        return valid;
    }
}
