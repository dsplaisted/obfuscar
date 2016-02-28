using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Obfuscar
{
    static class NameMaker2
    {
        static SHA256Managed _hasher = new SHA256Managed();

        public static string HashName(string name)
        {
            byte[] hash;
            lock (_hasher)
            {
                hash = _hasher.ComputeHash(Encoding.UTF8.GetBytes(name));
            }
            StringBuilder sb = new StringBuilder(hash.Length*2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
