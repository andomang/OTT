// ============================================================
// Updater.cs — 시작할 때 자동 업데이트
//
// 1) GitHub 최신 릴리스(andomang/OTT)의 버전을 현재 버전과 비교
// 2) 새 버전이면 릴리스에 올린 설치 파일(OTT-Setup-*.exe)을 받아 조용히 설치
// 3) 설치 프로그램이 앱을 닫고 C:\OTT의 파일을 바꾼 뒤 앱을 다시 실행
//
// 인터넷이 안 되거나 확인에 실패하면 업데이트 없이 그대로 실행합니다.
// ============================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersonalPro
{
    public static class Updater
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/andomang/OTT/releases/latest";

        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("OTT-Multi-View-Updater");  // GitHub API 필수 헤더
            return http;
        }

        public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);

        /// <summary>
        /// 새 버전이 있으면 사용자에게 묻고 설치를 시작합니다.
        /// </summary>
        /// <returns>설치를 시작해 앱을 종료해야 하면 true</returns>
        public static async Task<bool> CheckAndInstallAsync(IWin32Window owner)
        {
            var release = await GetLatestAsync();
            if (release is not { } r || !UpdateCheck.IsNewer(r.Tag, CurrentVersion)) return false;

            if (MessageBox.Show(owner,
                    $"새 버전 {r.Tag}이(가) 있습니다. (현재 {CurrentVersion.ToString(3)})\n지금 업데이트하시겠습니까?",
                    "업데이트", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                return false;

            string? setup = await DownloadAsync(owner, r.Url, r.Size);
            if (setup == null) return false;

            // /SILENT: 진행 창만 보여주고 질문 없이 설치. 설치가 끝나면 설치 프로그램이 앱을 다시 실행
            Process.Start(new ProcessStartInfo(setup, "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS")
            {
                UseShellExecute = true
            });
            Application.Exit();
            return true;
        }

        /// <summary>최신 릴리스의 태그와 설치 파일 주소. 실패하거나 설치 파일이 없으면 null</summary>
        private static async Task<(string Tag, string Url, long Size)?> GetLatestAsync()
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var doc = JsonDocument.Parse(await Http.GetStringAsync(LatestReleaseApi, cts.Token));
                string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

                var asset = doc.RootElement.GetProperty("assets").EnumerateArray().FirstOrDefault(a =>
                {
                    string name = a.GetProperty("name").GetString() ?? "";
                    return name.StartsWith("OTT-Setup", StringComparison.OrdinalIgnoreCase) &&
                           name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                });
                if (asset.ValueKind != JsonValueKind.Object) return null;  // 설치 파일 없는 예전 릴리스

                string url = asset.GetProperty("browser_download_url").GetString() ?? "";
                // 다른 곳에서 받은 파일을 실행하지 않도록 GitHub 주소만 허용
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                    uri.Host != "github.com") return null;

                return (tag, url, asset.GetProperty("size").GetInt64());
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                           or KeyNotFoundException or InvalidOperationException)
            {
                return null;  // 오프라인, GitHub 응답 이상 → 업데이트 건너뜀
            }
        }

        /// <summary>진행률 창을 띄우고 설치 파일을 임시 폴더에 받습니다. 실패하면 null</summary>
        private static async Task<string?> DownloadAsync(IWin32Window owner, string url, long size)
        {
            string path = Path.Combine(Path.GetTempPath(), "OTT-Setup-update.exe");
            var bar = new ProgressBar { Dock = DockStyle.Bottom, Height = 22, Maximum = 100 };
            using var form = new Form
            {
                Text = "업데이트 받는 중...", ClientSize = new Size(360, 70), FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen, ControlBox = false, ShowInTaskbar = true,
                Padding = new Padding(14)
            };
            form.Controls.Add(bar);
            form.Controls.Add(new Label { Text = "새 버전을 내려받고 있습니다.", Dock = DockStyle.Top });
            form.Show(owner);

            try
            {
                using var res = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                res.EnsureSuccessStatusCode();
                await using (var src = await res.Content.ReadAsStreamAsync())
                await using (var dst = File.Create(path))
                {
                    var buf = new byte[81920];
                    long done = 0;
                    int n;
                    while ((n = await src.ReadAsync(buf)) > 0)
                    {
                        await dst.WriteAsync(buf.AsMemory(0, n));
                        done += n;
                        if (size > 0) bar.Value = (int)Math.Min(100, done * 100 / size);
                    }
                    // 중간에 끊긴 파일을 실행하지 않도록 크기 확인
                    if (size > 0 && done != size) throw new IOException("다운로드가 완료되지 않았습니다.");
                }
                return path;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException
                                           or UnauthorizedAccessException)
            {
                form.Close();
                MessageBox.Show(owner, $"업데이트를 내려받지 못했습니다. 이번에는 현재 버전으로 실행합니다.\n{ex.Message}",
                    "업데이트", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }
    }
}
