using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PersonalPro
{
    public static class DatabaseHelper
    {
        // 사용자 전용 구글 웹 앱 URL
        private static readonly string scriptUrl = "https://script.google.com/macros/s/AKfycbzkqdl2JXOS2Z4o2vrPgwcEwTc4suEMz1xcNGs9x7NuGVGGVNp5kjJvZmveSg0UHAod2g/exec";

        public static void InitializeDatabase() { }

        // 로그인 (userIdx 반환)
        public static int LoginCheckReturnIdx(string id, string pw)
        {
            try
            {
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "login"), new("id", id), new("pw", pw)
                };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                string res = response.Content.ReadAsStringAsync().Result.Trim();
                return int.TryParse(res, out int idx) ? idx : -1;
            }
            catch { return -1; }
        }

        // 회원가입
        public static bool RegisterUser(string id, string pw)
        {
            try
            {
                using var client = new HttpClient();
                var pairs = new List<KeyValuePair<string, string>> {
                    new("action", "register"), new("id", id), new("pw", pw)
                };
                var response = client.PostAsync(scriptUrl, new FormUrlEncodedContent(pairs)).Result;
                return response.Content.ReadAsStringAsync().Result.Trim() == "success";
            }
            catch { return false; }
        }

        // OTT 리스트 가져오기
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