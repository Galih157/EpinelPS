using System.Security.Cryptography;
using System.Text;

namespace EpinelPS.Utils;

public static class PasswordHelper
{
    /// <summary>
    /// Computes the lowercase MD5 hex digest of a plaintext password.
    /// </summary>
    public static string HashMd5(string password)
    {
        return Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(password))).ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes an input that may already be a 32-character hex MD5 digest or plaintext.
    /// </summary>
    public static string NormalizeToMd5(string passwordOrHash)
    {
        if (IsMd5Hex(passwordOrHash))
            return passwordOrHash.ToLowerInvariant();

        return HashMd5(passwordOrHash);
    }

    /// <summary>
    /// Verifies an incoming password (plaintext or MD5 hex) against a stored MD5 hash.
    /// </summary>
    public static bool VerifyMd5(string input, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        if (storedHash.Equals(input, StringComparison.OrdinalIgnoreCase))
            return true;

        return storedHash.Equals(HashMd5(input), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMd5Hex(string value)
    {
        if (value.Length != 32)
            return false;

        foreach (char c in value)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }
        return true;
    }
}
