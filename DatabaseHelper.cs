using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PersonalPro
{
    public static class DatabaseHelper
    {
        // 사용자 전용 구글 웹 앱 URL (배포된 URL 유지)
        private static readonly string scriptUrl = "https://script.google.com/macros/s/AKfycbzkqdl2JXOS2Z4o2vrPgwcEwTc4suEMz1xcNGs9x7NuGVGGVNp5kjJvZmveSg0UHAod2g/exec";

        public static void InitializeDatabase() { }

        #region 보안 관련 (비밀번호 해싱)
        /// <summary>
        /// 비밀번호를 SHA-256 방식으로 암호화(해싱)합니다.
        /// </summary>
        private static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // 비밀번호를 바이트 배열로 변환 후 해시 계산
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // 바이트 배열을 16진수 문자열로 변환
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        #endregion

        // 1. 로그인 (암호화된 비번 전송)
        public static int LoginCheckReturnIdx(string id, string pw)
        {
            try
            {
                string hashedPw = HashPassword(pw); // 비번 암호화

                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "login"),
                    new("id", id),
                    new("pw", hashedPw) // 암호화된 값 전달
                };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                string res = response.Content.ReadAsStringAsync().Result.Trim();
                return int.TryParse(res, out int idx) ? idx : -1;
            }
            catch { return -1; }
        }

        // 2. 회원가입 (암호화된 비번 저장)
        public static bool RegisterUser(string id, string pw)
        {
            try
            {
                string hashedPw = HashPassword(pw); // 비번 암호화

                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "register"),
                    new("id", id),
                    new("pw", hashedPw) // 암호화된 값 저장 요청
                };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                return response.Content.ReadAsStringAsync().Result.Trim() == "success";
            }
            catch { return false; }
        }

        // 3. OTT 리스트 가져오기
        public static List<string[]> GetUserOtts(int userIdx)
        {
            var list = new List<string[]>();
            try
            {
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "getList"),
                    new("userIdx", userIdx.ToString())
                };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                string res = response.Content.ReadAsStringAsync().Result.Trim();

                if (!string.IsNullOrEmpty(res) && res != "-1")
                {
                    foreach (var item in res.Split(','))
                    {
                        var p = item.Split('|');
                        if (p.Length >= 3) list.Add(p);
                    }
                }
            }
            catch { }
            return list;
        }

        // 4. OTT 추가/수정/삭제
        public static void AddUserOtt(int userIdx, string name, string url) => SendRequest("add", userIdx.ToString(), name, url);
        public static void UpdateUserOtt(string dbIdx, string name, string url) => SendRequest("update", dbIdx, name, url);
        public static void DeleteUserOtt(string dbIdx) => SendRequest("delete", dbIdx);

        private static void SendRequest(string action, string p1, string p2 = "", string p3 = "")
        {
            try
            {
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> { new("action", action) };
                if (action == "add") { pairs.Add(new("userIdx", p1)); pairs.Add(new("name", p2)); pairs.Add(new("url", p3)); }
                else if (action == "update") { pairs.Add(new("dbIdx", p1)); pairs.Add(new("name", p2)); pairs.Add(new("url", p3)); }
                else if (action == "delete") { pairs.Add(new("dbIdx", p1)); }
                _ = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
            }
            catch { }
        }
    }
}