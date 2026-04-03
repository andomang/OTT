using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PersonalPro
{
    public class RegisterForm : Form
    {
        private TextBox txtId, txtPw, txtConfirm, txtNick;

        public RegisterForm()
        {
            this.Text = "회원가입";
            this.Size = new Size(300, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.KeyPreview = true;

            AddLabel("아이디", 20);
            txtId = AddTextBox(45, "아이디를 입력하세요");

            AddLabel("닉네임", 85);
            txtNick = AddTextBox(110, "사용할 닉네임");

            AddLabel("비밀번호", 150);
            txtPw = AddTextBox(175, "8자 이상+특수문자", true);

            AddLabel("비밀번호 확인", 215);
            txtConfirm = AddTextBox(240, "다시 입력하세요", true);

            Label lblNotice = new Label { Text = "* 8자 이상, 영문/숫자/특수문자 포함", Location = new Point(20, 275), ForeColor = Color.Gray, Font = new Font("맑은 고딕", 8), AutoSize = true };
            this.Controls.Add(lblNotice);

            Button btn = new Button { Text = "가입 완료", Location = new Point(20, 310), Size = new Size(240, 45), BackColor = Color.FromArgb(229, 9, 20), FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 10, FontStyle.Bold) };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += async (s, e) => await RegisterAction(btn);
            this.Controls.Add(btn);

            this.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await RegisterAction(btn); };
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

        private async Task RegisterAction(Button btn)
        {
            if (!btn.Enabled) return;
            string id = txtId.Text.Trim(); string nick = txtNick.Text.Trim(); string pw = txtPw.Text.Trim();
            if (id == txtId.Tag.ToString() || pw == txtPw.Tag.ToString()) { MessageBox.Show("정보를 입력해주세요."); return; }
            if (pw != txtConfirm.Text.Trim()) { MessageBox.Show("비밀번호가 일치하지 않습니다."); return; }
            if (!new Regex(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$").IsMatch(pw)) { MessageBox.Show("비밀번호 규칙 위반!"); return; }
            btn.Enabled = false;
            bool success = await Task.Run(() => DatabaseHelper.RegisterUser(id, pw, nick));
            if (success) { MessageBox.Show("가입 성공!"); this.Close(); }
            else { MessageBox.Show("중복된 아이디 혹은 오류"); btn.Enabled = true; }
        }
    }
}