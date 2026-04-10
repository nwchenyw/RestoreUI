using System;
using System.Security.Cryptography;
using System.Text;

namespace Restore.Core
{
    public static class AuthManager
    {
        public static string Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool Verify(string input, string hash)
        {
            return Hash(input) == hash;
        }
    }
}
