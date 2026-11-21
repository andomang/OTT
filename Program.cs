// ============================================================
// Program.cs — 앱의 시작점 (진입점)
// 이 파일이 가장 먼저 실행됩니다.
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersonalPro
{
    // internal = 이 프로젝트 안에서만 쓸 수 있는 클래스
    // static   = 직접 new로 만들 필요 없이 바로 사용하는 클래스
    internal static class Program
    {
        // [STAThread] = Windows 창(UI) 프로그램을 실행할 때 꼭 붙여야 하는 설정
        // static void Main() = 프로그램이 시작하면 제일 먼저 실행되는 메서드
        [STAThread]
        static void Main()
        {
            // 버튼·텍스트 등 화면 요소를 예쁘게 보이게 해주는 설정
            Application.EnableVisualStyles();

            // 글씨 렌더링 방식을 최신 방식으로 설정
            Application.SetCompatibleTextRenderingDefault(false);

            // MainForm(메인 창)을 열고 앱을 실행 — 창이 닫히면 앱도 종료됨
            Application.Run(new MainForm());
        }
    }
}
