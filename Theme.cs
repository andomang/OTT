// ============================================================
// Theme.cs — 다크 모드 색상 변환
//
// 화면은 밝은 테마 색으로 만들고, 다크 모드면 이 표로 색을 바꿉니다.
// ============================================================
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PersonalPro
{
    public static class Theme
    {
        public static bool Dark => AppSettings.Current.DarkMode;

        // 밝은 색 → 어두운 색
        private static readonly Dictionary<int, Color> LightToDark = new()
        {
            [Color.FromArgb(248, 249, 252).ToArgb()] = Color.FromArgb(24, 24, 32),    // 앱 배경
            [Color.White.ToArgb()]                   = Color.FromArgb(32, 32, 42),    // 패널
            [Color.FromArgb(255, 242, 242).ToArgb()] = Color.FromArgb(58, 30, 34),    // 사용자 헤더
            [Color.FromArgb(17, 24, 39).ToArgb()]    = Color.FromArgb(232, 234, 240), // 진한 텍스트
            [Color.FromArgb(75, 85, 99).ToArgb()]    = Color.FromArgb(176, 180, 192), // 중간 텍스트
            [Color.FromArgb(156, 163, 175).ToArgb()] = Color.FromArgb(130, 135, 150), // 연한 텍스트
            [Color.FromArgb(229, 231, 235).ToArgb()] = Color.FromArgb(58, 58, 72),    // 테두리
            [Color.FromArgb(243, 244, 246).ToArgb()] = Color.FromArgb(48, 48, 60),    // hover
            [Color.FromArgb(249, 250, 251).ToArgb()] = Color.FromArgb(40, 40, 52),    // 입력창
            [Color.FromArgb(158, 18, 18).ToArgb()]   = Color.FromArgb(255, 150, 150)  // 환영 문구
        };

        private static readonly Dictionary<int, Color> DarkToLight =
            LightToDark.ToDictionary(e => e.Value.ToArgb(), e => Color.FromArgb(e.Key));

        /// <summary>밝은 테마 색을 현재 테마 색으로 바꿉니다.</summary>
        public static Color C(Color light) =>
            Dark && LightToDark.TryGetValue(light.ToArgb(), out var dark) ? dark : light;

        /// <summary>컨트롤과 자식 전체를 현재 테마 색으로 바꿉니다. OTT 칸은 항상 어두워 제외합니다.</summary>
        public static void Apply(Control root)
        {
            if (root is OttCell) return;
            var map = Dark ? LightToDark : DarkToLight;

            if (map.TryGetValue(root.BackColor.ToArgb(), out var back)) root.BackColor = back;

            // 빨간 버튼의 흰 글자는 그대로 둠
            if (root.ForeColor.ToArgb() != Color.White.ToArgb() &&
                map.TryGetValue(root.ForeColor.ToArgb(), out var fore)) root.ForeColor = fore;

            if (root is Button b && map.TryGetValue(b.FlatAppearance.MouseOverBackColor.ToArgb(), out var hover))
                b.FlatAppearance.MouseOverBackColor = hover;

            foreach (Control child in root.Controls) Apply(child);
            root.Invalidate();
        }
    }
}
