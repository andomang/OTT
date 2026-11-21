using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PersonalPro
{
    /// <summary>
    /// 비밀번호 해싱 및 AES 암복호화 유틸리티.
    ///
    /// ▶ 비밀번호 저장 방식
    ///   - 랜덤 16바이트 Salt 생성 → password + salt를 합쳐 SHA-256 해싱
    ///   - Salt는 별도 저장되어 VerifyPassword 시 재사용됨
    ///
    /// ▶ AES 암호화 방식 (Encrypt / Decrypt)
    ///   - AES-256-CBC 모드
    ///   - IV를 매 암호화마다 랜덤 생성하여 앞 16바이트에 붙여 저장
    ///   - 고정 IV 사용 시 동일 입력 → 동일 암호문 노출 문제를 방지
    ///
    /// ※ AesKey는 소스에 하드코딩되어 있으므로 배포 시 환경변수 또는
    ///    보호된 설정 파일로 이동하는 것을 권장합니다.
    /// </summary>
    public static class SecurityHelper
    {
        // AES-256 키 (정확히 32바이트)
        // 실제 배포 시 소스 외부(환경변수, 설정파일)로 옮길 것
        private static readonly byte[] AesKey =
            Encoding.UTF8.GetBytes("PersonalProOttSecretKey123456789");

        // ── 비밀번호 해싱 ─────────────────────────────────────────────

        /// <summary>
        /// 비밀번호로부터 Salt와 Hash를 생성합니다.
        /// 반환된 (Hash, Salt)를 각각 DB에 저장하고 VerifyPassword 시 재사용합니다.
        /// </summary>
        /// <param name="password">평문 비밀번호</param>
        /// <returns>(Base64 해시, Base64 솔트) 튜플</returns>
        public static (string Hash, string Salt) GenerateHash(string password)
        {
            // .NET 6+ RandomNumberGenerator.GetBytes() — RNGCryptoServiceProvider 대체
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            string salt = Convert.ToBase64String(saltBytes);

            // password + salt 를 UTF-8 바이트로 결합 후 SHA-256 해싱
            byte[] combined = Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = SHA256.HashData(combined);  // .NET 5+ 정적 메서드

            return (Convert.ToBase64String(hashBytes), salt);
        }

        /// <summary>
        /// 입력된 비밀번호가 저장된 Hash·Salt와 일치하는지 검증합니다.
        /// </summary>
        /// <param name="enteredPw">사용자가 입력한 평문 비밀번호</param>
        /// <param name="storedHash">DB에 저장된 Base64 해시</param>
        /// <param name="storedSalt">DB에 저장된 Base64 솔트</param>
        public static bool VerifyPassword(string enteredPw, string storedHash, string storedSalt)
        {
            byte[] combined   = Encoding.UTF8.GetBytes(enteredPw + storedSalt);
            byte[] hashBytes  = SHA256.HashData(combined);
            return Convert.ToBase64String(hashBytes) == storedHash;
        }

        // ── AES 암복호화 ─────────────────────────────────────────────

        /// <summary>
        /// 텍스트를 AES-256-CBC로 암호화합니다.
        /// IV를 랜덤으로 생성하고, [16바이트 IV][암호문] 형태로 합쳐 Base64로 반환합니다.
        /// </summary>
        /// <param name="text">암호화할 평문</param>
        /// <returns>Base64 인코딩된 암호문 (IV 포함), 빈 문자열이면 그대로 반환</returns>
        public static string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            using var aes = Aes.Create();
            aes.Key = AesKey;

            // IV를 매번 새로 생성 — 동일 입력이라도 암호문이 달라짐
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            using var ms        = new MemoryStream();

            // 결과물: [IV 16바이트][암호문]
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(text);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Encrypt()로 암호화된 Base64 문자열을 복호화합니다.
        /// 앞 16바이트를 IV로 분리하여 AES-256-CBC 복호화를 수행합니다.
        /// </summary>
        /// <param name="cipherText">Base64 인코딩된 암호문 (IV 포함)</param>
        /// <returns>복호화된 평문, 실패 시 빈 문자열</returns>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                byte[] fullData = Convert.FromBase64String(cipherText);

                // 앞 16바이트 = IV, 나머지 = 실제 암호문
                byte[] iv         = new byte[16];
                byte[] cipherData = new byte[fullData.Length - 16];
                Buffer.BlockCopy(fullData, 0,  iv,         0, 16);
                Buffer.BlockCopy(fullData, 16, cipherData, 0, cipherData.Length);

                using var aes       = Aes.Create();
                aes.Key             = AesKey;
                aes.IV              = iv;
                using var decryptor = aes.CreateDecryptor();
                using var ms        = new MemoryStream(cipherData);
                using var cs        = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr        = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch
            {
                // 잘못된 키 또는 손상된 데이터 → 빈 문자열 반환
                return "";
            }
        }
    }
}
