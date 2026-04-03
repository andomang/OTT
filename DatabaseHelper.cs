using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace PersonalPro
{
    public static class DatabaseHelper
    {
        private static readonly string scriptUrl = "https://script.google.com/macros/s/AKfycby8RCvPsi8GMjqyTOReN4BH3u1Po9vdEuI2M9MqzwppQ-LkYkKUD3vK6VYrlRiRRNAUxg/exec";
        public static string CurrentNickname = ""; // 닉네임 저장용

        public static void InitializeDatabase() { }

        // SHA-256 해싱 (최신 방식 적용)
        private static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            // SHA256.HashData는 .NET 최신 버전에서 권장하는 정적 메서드입니다.
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        // 로그인
        public static int LoginCheckReturnIdx(string id, string pw)
        {
            try
            {
                string hashedPw = HashPassword(pw);
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "login"), new("id", id), new("pw", hashedPw)
                };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                string res = response.Content.ReadAsStringAsync().Result.Trim();

                if (res.Contains("|"))
                {
                    var parts = res.Split('|');
                    CurrentNickname = parts[1]; // 닉네임 추출
                    return int.Parse(parts[0]); // idx 추출
                }
                return -1;
            }
            catch { return -1; }
        }

        // 회원가입
        public static bool RegisterUser(string id, string pw, string nick)
        {
            try
            {
                string hashedPw = HashPassword(pw);
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "register"), new("id", id), new("pw", hashedPw), new("nickname", nick)
                };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                return response.Content.ReadAsStringAsync().Result.Trim() == "success";
            }
            catch { return false; }
        }

        // OTT 목록 가져오기
        public static List<string[]> GetUserOtts(int userIdx)
        {
            var list = new List<string[]>();
            try
            {
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "getList"), new("userIdx", userIdx.ToString())
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

        // OTT 추가
        public static void AddUserOtt(int userIdx, string name, string url) => SendRequest("add", userIdx.ToString(), name, url);

        // OTT 수정 (에러 해결용 추가)
        public static void UpdateUserOtt(string dbIdx, string name, string url) => SendRequest("update", dbIdx, name, url);

        // OTT 삭제
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
        // DatabaseHelper 클래스 안에 추가
        public static bool UpdateUserInfo(int userIdx, string pw, string nick)
        {
            try
            {
                string hashedPw = HashPassword(pw); // 새 비밀번호 암호화
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
            new("action", "updateUserInfo"),
            new("userIdx", userIdx.ToString()),
            new("pw", hashedPw),
            new("nickname", nick)
        };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                bool success = response.Content.ReadAsStringAsync().Result.Trim() == "success";
                if (success) CurrentNickname = nick; // 로컬 닉네임 정보도 갱신
                return success;
            }
            catch { return false; }
        }
    }
}