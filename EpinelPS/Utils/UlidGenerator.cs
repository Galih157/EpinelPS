using System.Security.Cryptography;

namespace EpinelPS.Utils;

public static class UlidGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Generates a 26-character Crockford Base32 ULID string.
    /// </summary>
    public static string NewUlid()
    {
        Span<byte> random = stackalloc byte[10];
        RandomNumberGenerator.Fill(random);

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<char> chars = stackalloc char[26];

        // 48-bit timestamp (10 chars)
        for (int i = 9; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(timestamp & 0x1F)];
            timestamp >>= 5;
        }

        // 80-bit randomness (16 chars)
        chars[10] = Alphabet[(random[0] & 0xF8) >> 3];
        chars[11] = Alphabet[((random[0] & 0x07) << 2) | ((random[1] & 0xC0) >> 6)];
        chars[12] = Alphabet[(random[1] & 0x3E) >> 1];
        chars[13] = Alphabet[((random[1] & 0x01) << 4) | ((random[2] & 0xF0) >> 4)];
        chars[14] = Alphabet[((random[2] & 0x0F) << 1) | ((random[3] & 0x80) >> 7)];
        chars[15] = Alphabet[(random[3] & 0x7C) >> 2];
        chars[16] = Alphabet[((random[3] & 0x03) << 3) | ((random[4] & 0xE0) >> 5)];
        chars[17] = Alphabet[random[4] & 0x1F];
        chars[18] = Alphabet[(random[5] & 0xF8) >> 3];
        chars[19] = Alphabet[((random[5] & 0x07) << 2) | ((random[6] & 0xC0) >> 6)];
        chars[20] = Alphabet[(random[6] & 0x3E) >> 1];
        chars[21] = Alphabet[((random[6] & 0x01) << 4) | ((random[7] & 0xF0) >> 4)];
        chars[22] = Alphabet[((random[7] & 0x0F) << 1) | ((random[8] & 0x80) >> 7)];
        chars[23] = Alphabet[(random[8] & 0x7C) >> 2];
        chars[24] = Alphabet[((random[8] & 0x03) << 3) | ((random[9] & 0xE0) >> 5)];
        chars[25] = Alphabet[random[9] & 0x1F];

        return new string(chars);
    }
}
