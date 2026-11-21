// ============================================================
// OttInput.cs — OTT 추가/수정 입력창과 입력값 검사
//
// 메인 창과 PiP 런처가 같은 규칙으로 OTT를 입력받도록 한 곳에 모음
// 서버 응답이 , 와 | 로 값을 구분하므로 두 문자는 입력할 수 없음
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace PersonalPro
{
    public static class OttInput
    {
        // 기본 OTT ([0]=DB인덱스(없음), [1]=이름, [2]=URL)
        // 사용자 OTT와 이름이 겹치면 화면이 섞이므로 같은 이름은 사용 불가
        private static readonly string[][] BuiltIns =
        {
            new[] { "", "NETFLIX", "https://www.netflix.com/login" },
            new[] { "", "YOUTUBE", "https://www.youtube.com" }
        };

        /// <summary>
        /// 기본 OTT와 사용자 OTT를 저장된 순서대로 반환합니다.
        /// </summary>
        public static List<string[]> AllOtts(string userId, IEnumerable<string[]> userOtts)
        {
            AppSettings.Current.OttOrder.TryGetValue(userId, out var order);
            return OttOrdering.Sort(BuiltIns.Concat(userOtts), o => o[1], order ?? new List<string>());
        }

        /// <summary>
        /// 이름과 URL을 입력받아 검사합니다. 취소했거나 값이 잘못되면 null을 반환합니다.
        /// </summary>
        public static (string Name, string Url)? Prompt(
            string title, string curName, string curUrl, IEnumerable<string[]> userOtts)
        {
            string name = Interaction.InputBox("OTT 이름:", title, curName).Trim();
            if (name == "") return null;
            string url = Interaction.InputBox("URL 주소:", title, curUrl).Trim();
            if (url == "") return null;

            var taken = BuiltIns.Select(o => o[1]).Concat(userOtts.Select(o => o[1]));
            string? error = OttValidator.Validate(name, url, curName, taken);
            if (error != null)
            {
                MessageBox.Show(error, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return (name, url);
        }

        /// <summary>
        /// 입력 내용이 가려지는 비밀번호 입력창을 띄웁니다. 취소하면 null을 반환합니다.
        /// </summary>
        public static string? AskPassword(string title, string message)
        {
            using var form = new Form
            {
                Text = title, ClientSize = new Size(320, 120), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
            };
            var label = new Label { Text = message, Location = new Point(14, 12), Size = new Size(292, 20) };
            var input = new TextBox { Location = new Point(14, 38), Width = 292, UseSystemPasswordChar = true };
            var ok     = new Button { Text = "확인", Location = new Point(146, 78), DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "취소", Location = new Point(231, 78), DialogResult = DialogResult.Cancel };
            form.Controls.AddRange(new Control[] { label, input, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            return form.ShowDialog() == DialogResult.OK && input.Text != "" ? input.Text : null;
        }

        /// <summary>서버 저장 실패를 사용자에게 알립니다.</summary>
        public static void ShowSaveError() =>
            MessageBox.Show("서버에 저장하지 못했습니다. 인터넷 연결을 확인하고 다시 시도해주세요.", "저장 실패",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
