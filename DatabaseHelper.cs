// ============================================================
// DatabaseHelper.cs — 서버(구글 스프레드시트)와 통신하는 창구
//
// 이 클래스가 하는 일:
//   1) 로그인 / 회원가입 / 정보 수정 요청을 서버에 보냄
//   2) OTT 목록 가져오기 / 추가 / 수정 / 삭제 요청을 서버에 보냄
//   3) 비밀번호는 SHA-256으로 암호화해서 보냄 (평문 전송 방지)
//
// 통신 방식: HTTP POST (마치 웹 브라우저가 폼을 제출하는 것과 같음)
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace PersonalPro
{
    // public  = 다른 클래스에서도 쓸 수 있음
    // static  = new로 만들 필요 없이 DatabaseHelper.메서드() 형태로 바로 사용
    public static class DatabaseHelper
    {
        // ── 서버 주소 ─────────────────────────────────────────────────
        // string = 문자열 타입
        // 구글 앱스 스크립트(서버)의 주소 — 여기로 데이터를 보내고 받음
        private static readonly string ScriptUrl =
            "https://script.google.com/macros/s/AKfycby-6oey4akpWU-UtN2O_eHUZ8zGdhvt_-zEuExN2iCpCouZqXnyp0n5JvkTLwOkVg5QYw/exec";

        // ── HTTP 클라이언트 ───────────────────────────────────────────
        // HttpClient = 인터넷으로 데이터를 주고받는 도구
        // static readonly = 앱 전체에서 하나만 만들어 재사용 (여러 개 만들면 소켓 자원 낭비)
        // Timeout = 15초 안에 서버가 응답 안 하면 포기
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)   // TimeSpan = 시간 간격을 나타내는 타입
        };

        // ── 현재 로그인한 사용자 닉네임 ──────────────────────────────
        // string = 문자열 타입
        // 로그인 성공 후 여기에 닉네임을 저장해두면 메인 화면에서 바로 꺼내 씀
        // { get; private set; } = 읽기는 어디서나 가능, 쓰기는 이 클래스 안에서만 가능
        public static string CurrentNickname { get; private set; } = "";

        // ── 로그인 토큰 ───────────────────────────────────────────────
        // 로그인하면 서버가 주는 임의의 문자열. 이후 모든 요청에 붙여서 "내가 누구인지" 증명함
        // (예전엔 사용자 번호만 보내서, 번호만 알면 남의 계정을 바꿀 수 있었음)
        private static string _token = "";

        // ── 초기화 메서드 (현재 비어있음) ────────────────────────────
        // 앱 시작 시 호출하는 초기화 메서드 — 나중에 쓸 수도 있어서 남겨둠
        public static void InitializeDatabase() { }

        // ── 로컬 저장 경로 ────────────────────────────────────────────

        /// <summary>
        /// AppData\Local\PersonalPro 아래 경로를 만듭니다.
        /// OTT 이름이나 아이디에 파일명으로 못 쓰는 문자가 있으면 _ 로 바꿉니다.
        /// </summary>
        public static string DataPath(params string[] parts)
        {
            return Path.Combine(
                new[] { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersonalPro" }
                    .Concat(parts.Select(PathNames.Sanitize)).ToArray());
        }

        // ── 자동 로그인 ───────────────────────────────────────────────
        // Windows DPAPI로 암호화 — 같은 Windows 계정에서만 복호화할 수 있음

        private static string AutoLoginPath => DataPath("autologin.dat");

        public static void SaveAutoLogin(string id, string pw)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AutoLoginPath)!);
            File.WriteAllBytes(AutoLoginPath, ProtectedData.Protect(
                Encoding.UTF8.GetBytes(id + "\n" + pw), null, DataProtectionScope.CurrentUser));
        }

        public static (string Id, string Pw)? LoadAutoLogin()
        {
            try
            {
                var parts = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    File.ReadAllBytes(AutoLoginPath), null, DataProtectionScope.CurrentUser)).Split('\n', 2);
                return parts.Length == 2 ? (parts[0], parts[1]) : null;
            }
            catch
            {
                return null;  // 파일 없음 또는 복호화 실패 → 일반 로그인
            }
        }

        public static void ClearAutoLogin()
        {
            if (File.Exists(AutoLoginPath)) File.Delete(AutoLoginPath);
        }

        // ── 비밀번호 암호화 ───────────────────────────────────────────

        /// <summary>
        /// 비밀번호를 SHA-256으로 암호화합니다.
        /// SHA-256 = 원본 글자를 알아볼 수 없는 64글자 문자열로 바꿔주는 암호화 방법
        /// 예: "abc123!" → "6ca13d52ca70c883..." 처럼 완전히 다른 글자열이 됨
        /// </summary>
        /// <param name="password">암호화할 평문 비밀번호 (string = 문자열)</param>
        /// <returns>암호화된 소문자 16진수 문자열 (string)</returns>
        private static string HashPassword(string password)
        {
            // 비밀번호가 비어있으면 빈 문자열 그대로 반환
            if (string.IsNullOrEmpty(password)) return "";

            // 문자열을 바이트 배열(byte[])로 변환 후 SHA-256으로 해시 계산
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));

            // byte[] → 소문자 16진수 문자열로 변환 후 반환
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// 서버에 HTTP POST 요청을 보내고 응답 문자열을 받아옵니다.
        /// 모든 서버 통신이 이 메서드를 거칩니다.
        /// </summary>
        /// <param name="pairs">보낼 데이터 목록 (키=값 쌍의 리스트 — List는 크기가 자유로운 목록)</param>
        /// <returns>서버 응답 문자열 (string). 통신 실패 시 null</returns>
        private static string? Post(List<KeyValuePair<string, string>> pairs)
        {
            try
            {
                // FormUrlEncodedContent = "action=login&id=홍길동" 형태로 데이터를 묶어주는 도구
                // .Result = 비동기 작업이 끝날 때까지 기다리기
                // 로그인했으면 토큰을 함께 보냄 (서버가 이 토큰으로 본인 확인)
                if (_token.Length > 0) pairs.Add(new("token", _token));
                var response = Http.PostAsync(ScriptUrl, new FormUrlEncodedContent(pairs)).Result;

                // 응답 본문을 문자열로 읽고 앞뒤 공백 제거
                string body = response.Content.ReadAsStringAsync().Result.Trim();

                // Apps Script 오류는 HTML 페이지로 오므로 실패로 처리
                return response.IsSuccessStatusCode && !body.StartsWith('<') ? body : null;
            }
            catch
            {
                // 네트워크 오류, 타임아웃 등 어떤 오류든 null 반환
                return null;
            }
        }

        // ── 로그인 / 회원가입 ─────────────────────────────────────────

        /// <summary>
        /// 아이디와 비밀번호로 로그인합니다.
        /// 성공하면 사용자 번호(int)를 반환하고 CurrentNickname도 업데이트합니다.
        /// </summary>
        /// <param name="id">사용자 아이디 (string = 문자열)</param>
        /// <param name="pw">평문 비밀번호 — 내부에서 SHA-256 암호화 후 전송 (string)</param>
        /// <returns>
        ///   성공: 사용자 DB 인덱스 번호 (int = 정수, 0 이상의 값)
        ///   아이디/비밀번호 불일치: -1
        ///   서버 통신 실패: -2
        /// </returns>
        public static int LoginCheckReturnIdx(string id, string pw)
        {
            // 서버에 action=login, id=..., pw=(암호화됨) 을 전송
            string? res = Post(new List<KeyValuePair<string, string>>
            {
                new("action", "login"),          // 서버에게 "로그인 처리를 해줘"라고 알림
                new("id",     id),               // 아이디
                new("pw",     HashPassword(pw))  // 비밀번호는 반드시 암호화 후 전송
            });

            if (res == null) return -2;

            // 서버 응답 형식: "사용자번호|토큰|닉네임" (예: "3|a1b2...|홍길동")
            // 닉네임에 | 가 있어도 되도록 앞의 두 칸만 자름
            var parts = res.Split('|', 3);
            if (parts.Length == 3 && int.TryParse(parts[0], out int idx))
            {
                _token = parts[1];             // 이후 요청에 쓸 토큰
                CurrentNickname = parts[2];
                return idx;   // 사용자 번호 반환
            }
            // 예전 서버("번호|닉네임", 토큰 없음): 로그인은 성공으로 처리 (서버를 새 버전으로 배포하기 전에도 "비밀번호 틀림"이 뜨지 않게)
            if (parts.Length == 2 && int.TryParse(parts[0], out idx))
            {
                _token = "";
                CurrentNickname = parts[1];
                return idx;
            }
            return -1;   // 로그인 실패 → -1 반환
        }

        /// <summary>
        /// 신규 계정을 등록합니다.
        /// 서버에서 아이디 중복도 함께 검사합니다.
        /// </summary>
        /// <param name="id">사용할 아이디 (string)</param>
        /// <param name="pw">비밀번호 — 내부에서 암호화 후 전송 (string)</param>
        /// <param name="nick">사용할 닉네임 (string)</param>
        /// <returns>성공: true / 아이디 중복 또는 서버 오류: false (bool = 참/거짓)</returns>
        public static bool RegisterUser(string id, string pw, string nick)
        {
            string? res = Post(new List<KeyValuePair<string, string>>
            {
                new("action",   "register"),
                new("id",       id),
                new("pw",       HashPassword(pw)),  // 암호화 필수
                new("nickname", nick)
            });

            // 서버가 "success" 문자열을 반환하면 성공 (true), 아니면 실패 (false)
            return res == "success";
        }

        // ── 사용자 정보 수정 ──────────────────────────────────────────

        /// <summary>
        /// 닉네임과 비밀번호를 동시에 변경합니다.
        /// 성공하면 CurrentNickname도 즉시 갱신되어 재로그인 없이 화면에 반영됩니다.
        /// </summary>
        /// <param name="userIdx">사용자 번호 (int = 정수)</param>
        /// <param name="pw">새 비밀번호 (string)</param>
        /// <param name="nick">새 닉네임 (string)</param>
        /// <returns>성공: true / 실패: false (bool)</returns>
        public static bool UpdateUserInfo(int userIdx, string pw, string nick)
        {
            string? res = Post(new List<KeyValuePair<string, string>>
            {
                new("action",   "updateUserInfo"),
                new("userIdx",  userIdx.ToString()),  // int를 string으로 변환해서 전송
                new("pw",       HashPassword(pw)),
                new("nickname", nick)
            });

            if (res == "success")
            {
                CurrentNickname = nick;  // 앱 안에서도 즉시 닉네임 갱신
                return true;
            }
            return false;
        }

        /// <summary>
        /// 닉네임만 변경합니다 (비밀번호는 그대로 유지).
        /// </summary>
        /// <param name="userIdx">사용자 번호 (int)</param>
        /// <param name="nick">새 닉네임 (string)</param>
        /// <returns>성공: true / 실패: false (bool)</returns>
        public static bool UpdateNickname(int userIdx, string nick)
        {
            string? res = Post(new List<KeyValuePair<string, string>>
            {
                new("action",   "updateNickname"),
                new("userIdx",  userIdx.ToString()),
                new("nickname", nick)
            });

            if (res == "success")
            {
                CurrentNickname = nick;  // 앱 안에서도 즉시 닉네임 갱신
                return true;
            }
            return false;
        }

        /// <summary>
        /// 비밀번호만 변경합니다 (닉네임은 그대로 유지).
        /// </summary>
        /// <param name="userIdx">사용자 번호 (int)</param>
        /// <param name="pw">새 비밀번호 (string)</param>
        /// <returns>성공: true / 실패: false (bool)</returns>
        public static bool UpdatePassword(int userIdx, string pw)
        {
            string? res = Post(new List<KeyValuePair<string, string>>
            {
                new("action",  "updatePassword"),
                new("userIdx", userIdx.ToString()),
                new("pw",      HashPassword(pw))  // 암호화 필수
            });
            return res == "success";
        }

        // ── OTT 목록 관리 (CRUD) ─────────────────────────────────────
        // CRUD = Create(추가) Read(조회) Update(수정) Delete(삭제)

        /// <summary>
        /// 사용자가 추가한 OTT 목록을 서버에서 가져옵니다.
        /// </summary>
        /// <param name="userIdx">사용자 번호 (int)</param>
        /// <returns>
        ///   List&lt;string[]&gt; = OTT 항목들의 목록
        ///   각 항목은 string[] 배열: [0]=DB인덱스, [1]=이름, [2]=URL
        ///   OTT가 없으면 빈 리스트, 서버 통신 실패면 null 반환
        /// </returns>
        public static List<string[]>? GetUserOtts(int userIdx)
        {
            // 빈 리스트 준비 (OTT가 없을 때 반환)
            var list = new List<string[]>();

            string? res = Post(new List<KeyValuePair<string, string>>
            {
                new("action",  "getList"),
                new("userIdx", userIdx.ToString())
            });

            if (res == null) return null;

            // 서버 응답이 비어있거나 "-1"이면 OTT 없음 → 빈 리스트 반환
            if (string.IsNullOrEmpty(res) || res == "-1") return list;

            // 서버 응답 형식: "1|넷플릭스|https://...,2|유튜브|https://..."
            // , 기호로 각 OTT 항목을 나누고, | 기호로 각 필드를 나눔
            foreach (var item in res.Split(','))
            {
                var parts = item.Split('|');

                // 필드가 3개 이상일 때만 유효한 데이터로 취급
                if (parts.Length >= 3) list.Add(parts);
            }
            return list;
        }

        /// <summary>
        /// 새 OTT를 목록에 추가합니다.
        /// </summary>
        /// <param name="userIdx">사용자 번호 (int)</param>
        /// <param name="name">OTT 이름 (string, 예: "웨이브")</param>
        /// <param name="url">OTT 주소 (string, 예: "https://www.wavve.com")</param>
        /// <returns>서버 통신 성공: true / 실패: false (bool)</returns>
        public static bool AddUserOtt(int userIdx, string name, string url)
        {
            return Post(new List<KeyValuePair<string, string>>
            {
                new("action",  "add"),
                new("userIdx", userIdx.ToString()),
                new("name",    name),
                new("url",     url)
            }) == "success";
        }

        /// <summary>
        /// 기존 OTT의 이름과 주소를 수정합니다.
        /// </summary>
        /// <param name="dbIdx">수정할 OTT의 DB 인덱스 번호 (string)</param>
        /// <param name="name">새 OTT 이름 (string)</param>
        /// <param name="url">새 OTT 주소 (string)</param>
        /// <returns>서버 통신 성공: true / 실패: false (bool)</returns>
        public static bool UpdateUserOtt(string dbIdx, string name, string url)
        {
            return Post(new List<KeyValuePair<string, string>>
            {
                new("action", "update"),
                new("dbIdx",  dbIdx),
                new("name",   name),
                new("url",    url)
            }) == "success";
        }

        /// <summary>
        /// OTT를 목록에서 삭제합니다.
        /// </summary>
        /// <param name="dbIdx">삭제할 OTT의 DB 인덱스 번호 (string)</param>
        /// <returns>서버 통신 성공: true / 실패: false (bool)</returns>
        public static bool DeleteUserOtt(string dbIdx)
        {
            return Post(new List<KeyValuePair<string, string>>
            {
                new("action", "delete"),
                new("dbIdx",  dbIdx)
            }) == "success";
        }

        // ── 계정 관리 ─────────────────────────────────────────────────

        /// <summary>복구용 이메일을 등록합니다. 비밀번호가 맞아야 합니다.</summary>
        public static bool SetEmail(string id, string pw, string email)
        {
            return Post(new List<KeyValuePair<string, string>>
            {
                new("action", "setEmail"),
                new("id",     id),
                new("pw",     HashPassword(pw)),
                new("email",  email)
            }) == "success";
        }

        /// <summary>아이디와 등록 이메일이 맞으면 임시 비밀번호를 메일로 보냅니다.</summary>
        public static bool ResetPassword(string id, string email)
        {
            return Post(new List<KeyValuePair<string, string>>
            {
                new("action", "resetPassword"),
                new("id",     id),
                new("email",  email)
            }) == "success";
        }

        /// <summary>계정과 서버의 OTT·레이아웃을 삭제합니다. 비밀번호가 맞아야 합니다.</summary>
        public static bool DeleteAccount(string id, string pw)
        {
            return Post(new List<KeyValuePair<string, string>>
            {
                new("action", "deleteAccount"),
                new("id",     id),
                new("pw",     HashPassword(pw))
            }) == "success";
        }

        // ── 레이아웃 서버 저장 ────────────────────────────────────────

        /// <summary>레이아웃을 서버에 저장합니다.</summary>
        public static bool SaveLayout(int userIdx, string name, string slots)
        {
            return Post(new List<KeyValuePair<string, string>>
            {
                new("action",  "saveLayout"),
                new("userIdx", userIdx.ToString()),
                new("name",    name),
                new("slots",   slots)
            }) == "success";
        }

        /// <summary>서버의 레이아웃 목록을 가져옵니다. 통신 실패나 구버전 서버면 null을 반환합니다.</summary>
        public static List<(string Name, string Slots)>? GetLayouts(int userIdx)
        {
            string? res = Post(new List<KeyValuePair<string, string>>
            {
                new("action",  "getLayouts"),
                new("userIdx", userIdx.ToString())
            });
            if (res == null || res == "unknown") return null;

            var list = new List<(string, string)>();
            foreach (var line in res.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('\t');
                if (parts.Length == 2) list.Add((parts[0], parts[1]));
            }
            return list;
        }
    }
}
