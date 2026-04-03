using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PersonalPro
{
    public class UpdateUserForm : Form
    {
        private TextBox txtNick, txtPw, txtConfirm;
        private int userIdx;

        public UpdateUserForm(int uIdx, string currentNick)
        {
            this.userIdx = uIdx;
            this.Text = "내 정보 수정";
            this.Size = new Size(300, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.KeyPreview = true;

            AddLabel("변경할 닉네임", 20);
            txtNick = AddTextBox(45, "새 닉네임");
            txtNick.Text = currentNick; txtNick.ForeColor = Color.White;

            AddLabel("새 비밀번호", 100);
            txtPw = AddTextBox(125, "8자 이상+특수문자", true);

            AddLabel("비밀번호 확인", 180);
            txtConfirm = AddTextBox(205, "비밀번호 확인", true);

            Button btn = new Button { Text = "수정 완료", Location = new Point(20, 270), Size = new Size(240, 45), BackColor = Color.FromArgb(229, 9, 20), FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 10, FontStyle.Bold) };
            btn.Click += async (s, e) => await UpdateAction(btn);
            this.Controls.Add(btn);

            this.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await UpdateAction(btn); };
        }

        private void AddLabel(string text, int y) => this.Controls.Add(new Label { Text = text, Location = new Point(20, y), AutoSize = true });

        private TextBox AddTextBox(int y, string placeholder, bool isPw = false)
        {
            TextBox tb = new TextBox { Location = new Point(20, y), Width = 240, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.Gray, Text = placeholder, Tag = placeholder };
            tb.Enter += (s, e) => { if (tb.Text == tb.Tag.ToString()) { tb.Text = ""; tb.ForeColor = Color.White; if (isPw) tb.UseSystemPasswordChar = true; } };
            tb.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = tb.Tag.ToString(); tb.ForeColor = Color.Gray; if (isPw) tb.UseSystemPasswordChar = false; } };
            this.Controls.Add(tb);
            return tb;
        }

        private async Task UpdateAction(Button btn)
        {
            if (!btn.Enabled) return;
            string nick = txtNick.Text.Trim(); string pw = txtPw.Text.Trim();
            if (nick == txtNick.Tag.ToString() || pw == txtPw.Tag.ToString()) { MessageBox.Show("정보를 입력해주세요."); return; }
            if (pw != txtConfirm.Text.Trim()) { MessageBox.Show("비밀번호가 일치하지 않습니다."); return; }
            if (!new Regex(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$").IsMatch(pw)) { MessageBox.Show("규칙 위반!"); return; }
            btn.Enabled = false;
            bool success = await Task.Run(() => DatabaseHelper.UpdateUserInfo(userIdx, pw, nick));
            if (success) { MessageBox.Show("수정 완료!"); this.DialogResult = DialogResult.OK; this.Close(); }
            else { MessageBox.Show("수정 실패!"); btn.Enabled = true; }
        }
    }
}