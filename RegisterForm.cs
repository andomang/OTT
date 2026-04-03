using System;
using System.Drawing;
using System.Windows.Forms;

namespace PersonalPro
{
    public class RegisterForm : Form
    {
        private TextBox txtNewId, txtNewPw;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegisterForm));
            this.SuspendLayout();
            // 
            // RegisterForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RegisterForm";
            this.ResumeLayout(false);

        }

        public RegisterForm()
        {
            this.Text = "회원가입";
            this.Size = new Size(300, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label lbl = new Label { Text = "ID", Location = new Point(20, 20), AutoSize = true };
            txtNewId = new TextBox { Location = new Point(20, 45), Width = 240, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White };
            Label lbl2 = new Label { Text = "Password", Location = new Point(20, 85), AutoSize = true };
            txtNewPw = new TextBox { Location = new Point(20, 110), Width = 240, BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White, UseSystemPasswordChar = true };

            Button btn = new Button { Text = "가입 완료", Location = new Point(20, 170), Size = new Size(240, 40), BackColor = Color.FromArgb(229, 9, 20), FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => {
                if (DatabaseHelper.RegisterUser(txtNewId.Text, txtNewPw.Text)) { MessageBox.Show("성공!"); this.Close(); }
                else MessageBox.Show("중복된 아이디입니다.");
            };
            this.Controls.AddRange(new Control[] { lbl, txtNewId, lbl2, txtNewPw, btn });
        }
    }
}