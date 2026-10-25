// Copyright 2026 URAV ADVANCED LEARNING SYSTEMS PRIVATE LIMITED
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;
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

        return Convert.ToHexStringLower(hash);
    }

    public static string Md5File(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hashed = MD5.HashData(stream);
        return Convert.ToHexStringLower(hashed);
    }
}
