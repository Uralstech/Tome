using System.Security.Cryptography;
using System.Text;

namespace Tome.Services;

internal static class CryptographyUtil
{
    public static string Md5(string input)
    {
        int utf8Count = Encoding.UTF8.GetByteCount(input);
        Span<byte> utf8 = utf8Count <= 512
            ? stackalloc byte[utf8Count]
            : new byte[utf8Count];

        Encoding.UTF8.GetBytes(input, utf8);

        Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
        MD5.HashData(utf8, hash);

        return Convert.ToHexString(hash);
    }
}
