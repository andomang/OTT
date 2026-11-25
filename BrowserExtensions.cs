// ============================================================
// BrowserExtensions.cs — 크롬 확장 프로그램 불러오기
//
// 실행 파일 옆 Extensions 폴더 안의 확장(manifest.json이 있는 폴더)을
// 메인 화면과 PiP의 WebView2에 불러옵니다.
// ============================================================
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace PersonalPro
{
    public static class BrowserExtensions
    {
        private static string Folder => Path.Combine(AppContext.BaseDirectory, "Extensions");

        /// <summary>
        /// 확장 사용을 켠 브라우저 옵션.
        /// 같은 데이터 폴더를 쓰는 메인 화면과 PiP가 같은 옵션을 써야 함께 열 수 있습니다.
        /// </summary>
        public static CoreWebView2EnvironmentOptions Options() => new() { AreBrowserExtensionsEnabled = true };

        /// <summary>
        /// Extensions 폴더의 확장을 설치합니다 (설치는 OTT별 브라우저 저장소에 남음).
        /// 이미 설치됐거나 지원되지 않아 실패해도 사이트는 열리도록 예외를 밖으로 내보내지 않습니다.
        /// </summary>
        public static async Task LoadAsync(CoreWebView2 core)
        {
            if (!Directory.Exists(Folder)) return;
            foreach (string manifest in Directory.GetFiles(Folder, "manifest.json", SearchOption.AllDirectories))
            {
                try
                {
                    await core.Profile.AddBrowserExtensionAsync(Path.GetDirectoryName(manifest)!);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"확장 프로그램 불러오기 실패 ({manifest}): {ex.Message}");
                }
            }
        }
    }
}
