using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PersonalPro
{
    public class SecurityHelper
    {
        private static readonly string AesKey = "PersonalProOttSecretKey123456789"; // 정확히 32글자

        public static (string Hash, string Salt) GenerateHash(string password)
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider()) { rng.GetBytes(saltBytes); }
            string salt = Convert.ToBase64String(saltBytes);
            using (var sha = SHA256.Create())
            {
                byte[] combinedBytes = Encoding.UTF8.GetBytes(password + salt);
                return (Convert.ToBase64String(sha.ComputeHash(combinedBytes)), salt);
            }
        }

        public static bool VerifyPassword(string enteredPw, string storedHash, string storedSalt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] combinedBytes = Encoding.UTF8.GetBytes(enteredPw + storedSalt);
                return Convert.ToBase64String(sha.ComputeHash(combinedBytes)) == storedHash;
            }
        }

        public static string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            byte[] iv = new byte[16];
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(AesKey);
                aes.IV = iv;
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs)) { sw.Write(text); }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(AesKey);
                    aes.IV = new byte[16];
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs)) { return sr.ReadToEnd(); }
                }
            }
            catch { return ""; }
        }
    }
}