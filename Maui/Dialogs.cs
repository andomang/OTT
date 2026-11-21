// ============================================================
// Dialogs.cs — OTT 목록·입력창·자동 로그인 (Windows 앱의 OttInput.cs에 해당)
// ============================================================
using PersonalPro;

namespace OttMaui;

public static class Dialogs
{
    // 기본 OTT ([0]=DB인덱스(없음), [1]=이름, [2]=URL) — Windows 앱과 같음
    private static readonly string[][] BuiltIns =
    {
        new[] { "", "NETFLIX", "https://www.netflix.com/login" },
        new[] { "", "YOUTUBE", "https://www.youtube.com" }
    };

    /// <summary>기본 OTT와 사용자 OTT를 저장된 순서대로 반환합니다.</summary>
    public static List<string[]> AllOtts(string userId, IEnumerable<string[]> userOtts)
    {
        AppSettings.Current.OttOrder.TryGetValue(userId, out var order);
        return OttOrdering.Sort(BuiltIns.Concat(userOtts), o => o[1], order ?? new List<string>());
    }

    /// <summary>이름과 URL을 입력받아 검사합니다. 취소했거나 값이 잘못되면 null을 반환합니다.</summary>
    public static async Task<(string Name, string Url)?> PromptOttAsync(
        Page page, string title, string curName, string curUrl, IEnumerable<string[]> userOtts)
    {
        string? name = (await page.DisplayPromptAsync(title, "OTT 이름:", "다음", "취소", initialValue: curName))?.Trim();
        if (string.IsNullOrEmpty(name)) return null;
        string? url = (await page.DisplayPromptAsync(title, "URL 주소:", "확인", "취소",
            initialValue: curUrl, keyboard: Keyboard.Url))?.Trim();
        if (string.IsNullOrEmpty(url)) return null;

        var taken = BuiltIns.Select(o => o[1]).Concat(userOtts.Select(o => o[1]));
        if (OttValidator.Validate(name, url, curName, taken) is { } error)
        {
            await page.DisplayAlertAsync(title, error, "확인");
            return null;
        }
        return (name, url);
    }

    public static Task ShowSaveError(Page page) =>
        page.DisplayAlertAsync("저장 실패", "서버에 저장하지 못했습니다. 인터넷 연결을 확인하고 다시 시도해주세요.", "확인");

    // ── 자동 로그인 ───────────────────────────────────────────────
    // Windows: DPAPI 파일 (Windows 앱과 같은 파일이라 두 앱이 정보를 공유)
    // 맥: 키체인 (SecureStorage)

    public static async Task SaveAutoLoginAsync(string id, string pw)
    {
#if WINDOWS
        DatabaseHelper.SaveAutoLogin(id, pw);
        await Task.CompletedTask;
#else
        await SecureStorage.Default.SetAsync("autologin", id + "\n" + pw);
#endif
    }

    public static async Task<(string Id, string Pw)?> LoadAutoLoginAsync()
    {
#if WINDOWS
        await Task.CompletedTask;
        return DatabaseHelper.LoadAutoLogin();
#else
        try
        {
            var parts = (await SecureStorage.Default.GetAsync("autologin"))?.Split('\n', 2);
            return parts is { Length: 2 } ? (parts[0], parts[1]) : null;
        }
        catch { return null; }
#endif
    }

    public static void ClearAutoLogin()
    {
#if WINDOWS
        DatabaseHelper.ClearAutoLogin();
#else
        SecureStorage.Default.Remove("autologin");
#endif
    }
}

/// <summary>
/// 페이지 안 영상/오디오의 음소거와 음량을 바꾸는 스크립트.
/// 사이트 자체 음량은 사용자가 Vol을 바꿨을 때만 건드립니다 (소리가 오르내리는 문제 방지).
/// </summary>
public static class WebMedia
{
    /// <param name="muted">음소거 여부</param>
    /// <param name="keepVolume">이후 새로 재생되는 영상에도 적용할 음량 (null = 사이트에 맡김)</param>
    /// <param name="setVolume">지금 재생 중인 영상에 한 번 적용할 음량 (null = 바꾸지 않음)</param>
    public static string Script(bool muted, double? keepVolume, double? setVolume)
    {
        static string N(double? v) => v?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        return "(function(m,k,s){window.__ottMuted=m;window.__ottVol=k;" +
               "function a(x,v){try{if(window.__ottMuted){if(!x.muted){x.muted=true;x.__ottM=1}}" +
               "else if(x.__ottM){x.muted=false;x.__ottM=0}if(v!=null)x.volume=v}catch(_){}}" +
               "window.__ottApply=a;" +
               "if(!window.__ottHooked){window.__ottHooked=1;document.addEventListener('play'," +
               "function(e){window.__ottApply(e.target,window.__ottVol)},true)}" +
               "document.querySelectorAll('video,audio').forEach(function(x){a(x,s)})})" +
               $"({(muted ? "true" : "false")},{N(keepVolume)},{N(setVolume)});";
    }
}
