using System.Drawing;
using System.Windows.Forms;

namespace PersonalPro
{
    partial class RegisterForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── 컨트롤 선언 ──────────────────────────────────────────────
        internal Panel  pnlDragBar;
        internal Button btnXClose;

        internal Label   lblTitle;
        internal Label   lblSubtitle;

        internal Label   lblIdField;
        internal TextBox txtId;

        internal Label   lblNickField;
        internal TextBox txtNick;

        internal Label   lblPwField;
        internal TextBox txtPw;

        internal Label   lblConfirmField;
        internal TextBox txtConfirm;

        internal Label   lblPasswordRule;
        internal Label   lblError;

        internal Button  btnRegister;
        internal Button  btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ── 색상 팔레트 (화이트 테마) ─────────────────────────────
            Color bgColor    = Color.FromArgb(244, 245, 250);
            Color cardColor  = Color.White;
            Color inputBg    = Color.FromArgb(247, 248, 252);
            Color accentRed  = Color.FromArgb(229, 9, 20);
            Color textDark   = Color.FromArgb(22, 22, 28);
            Color textGray   = Color.FromArgb(150, 150, 160);
            Color textMid    = Color.FromArgb(95, 95, 105);
            Color borderCol  = Color.FromArgb(226, 227, 234);
            Color hoverBg    = Color.FromArgb(243, 244, 248);

            Font fontTitle  = new Font("Segoe UI", 17f, FontStyle.Bold);
            Font fontSub    = new Font("Segoe UI", 8.5f);
            Font fontLabel  = new Font("Segoe UI", 8f, FontStyle.Bold);
            Font fontInput  = new Font("Segoe UI", 10f);
            Font fontBtn    = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            Font fontBtnSub = new Font("Segoe UI", 8.5f);
            Font fontRule   = new Font("Segoe UI", 7.5f);

            this.SuspendLayout();

            // ── Form ─────────────────────────────────────────────────
            this.Text            = "회원가입";
            this.ClientSize      = new Size(420, 580);
            this.MinimumSize     = new Size(420, 580);
            this.MaximumSize     = new Size(420, 580);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = bgColor;
            this.ForeColor       = textDark;
            this.FormBorderStyle = FormBorderStyle.None;
            this.KeyPreview      = true;

            // ── 드래그 바 ─────────────────────────────────────────────
            pnlDragBar = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(420, 44),
                BackColor = cardColor
            };

            btnXClose = new Button
            {
                Text      = "✕",
                Location  = new Point(376, 7),
                Size      = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = textMid,
                BackColor = cardColor,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnXClose.FlatAppearance.BorderSize = 0;
            btnXClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 9, 20);
            btnXClose.MouseEnter += (s, e) => { btnXClose.BackColor = accentRed; btnXClose.ForeColor = Color.White; };
            btnXClose.MouseLeave += (s, e) => { btnXClose.BackColor = cardColor; btnXClose.ForeColor = textMid; };

            Label lblDragHint = new Label
            {
                Text      = "OTT Multi-View — 회원가입",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = textGray,
                Location  = new Point(14, 13),
                AutoSize  = true
            };
            pnlDragBar.Controls.Add(lblDragHint);
            pnlDragBar.Controls.Add(btnXClose);

            // 드래그 바 하단 구분선 (Paint)
            pnlDragBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(borderCol), 0, 43, pnlDragBar.Width, 43);

            // ── 카드 패널 ─────────────────────────────────────────────
            Panel pnlCard = new Panel
            {
                Location  = new Point(20, 52),
                Size      = new Size(380, 516),
                BackColor = cardColor
            };

            // 타이틀
            lblTitle = new Label
            {
                Text     = "계정 만들기",
                Font     = fontTitle,
                ForeColor = textDark,
                Location = new Point(24, 24),
                Size     = new Size(332, 36),
                AutoSize = false
            };
            lblSubtitle = new Label
            {
                Text      = "OTT Multi-View에 오신 걸 환영합니다",
                Font      = fontSub,
                ForeColor = textMid,
                Location  = new Point(24, 62),
                AutoSize  = true
            };
            Panel divider = new Panel
            {
                Location  = new Point(24, 86),
                Size      = new Size(332, 1),
                BackColor = borderCol
            };

            // ── 아이디 ────────────────────────────────────────────────
            lblIdField = new Label
            {
                Text      = "아이디",
                Font      = fontLabel,
                ForeColor = textMid,
                Location  = new Point(24, 102),
                AutoSize  = true
            };
            txtId = new TextBox
            {
                Location    = new Point(24, 120),
                Size        = new Size(332, 36),
                BackColor   = inputBg,
                ForeColor   = textGray,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = fontInput,
                Text        = "아이디를 입력하세요",
                Tag         = "아이디를 입력하세요"
            };

            // ── 닉네임 ────────────────────────────────────────────────
            lblNickField = new Label
            {
                Text      = "닉네임",
                Font      = fontLabel,
                ForeColor = textMid,
                Location  = new Point(24, 168),
                AutoSize  = true
            };
            txtNick = new TextBox
            {
                Location    = new Point(24, 186),
                Size        = new Size(332, 36),
                BackColor   = inputBg,
                ForeColor   = textGray,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = fontInput,
                Text        = "사용할 닉네임",
                Tag         = "사용할 닉네임"
            };

            // ── 비밀번호 ──────────────────────────────────────────────
            lblPwField = new Label
            {
                Text      = "비밀번호",
                Font      = fontLabel,
                ForeColor = textMid,
                Location  = new Point(24, 234),
                AutoSize  = true
            };
            txtPw = new TextBox
            {
                Location    = new Point(24, 252),
                Size        = new Size(332, 36),
                BackColor   = inputBg,
                ForeColor   = textGray,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = fontInput,
                Text        = "8자 이상 + 특수문자",
                Tag         = "8자 이상 + 특수문자"
            };

            // ── 비밀번호 확인 ──────────────────────────────────────────
            lblConfirmField = new Label
            {
                Text      = "비밀번호 확인",
                Font      = fontLabel,
                ForeColor = textMid,
                Location  = new Point(24, 300),
                AutoSize  = true
            };
            txtConfirm = new TextBox
            {
                Location    = new Point(24, 318),
                Size        = new Size(332, 36),
                BackColor   = inputBg,
                ForeColor   = textGray,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = fontInput,
                Text        = "비밀번호를 다시 입력하세요",
                Tag         = "비밀번호를 다시 입력하세요"
            };

            // ── 규칙 안내 / 에러 ──────────────────────────────────────
            lblPasswordRule = new Label
            {
                Text      = "영문·숫자·특수문자(@$!%*#?&) 포함, 최소 8자",
                Font      = fontRule,
                ForeColor = Color.FromArgb(180, 182, 190),
                Location  = new Point(24, 360),
                Size      = new Size(332, 16),
                AutoSize  = false
            };
            lblError = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = accentRed,
                Location  = new Point(24, 378),
                Size      = new Size(332, 18),
                AutoSize  = false
            };

            // ── 버튼 ──────────────────────────────────────────────────
            btnRegister = new Button
            {
                Text      = "가입 완료",
                Location  = new Point(24, 406),
                Size      = new Size(332, 50),
                BackColor = accentRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = fontBtn,
                Cursor    = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.FlatAppearance.MouseOverBackColor = Color.FromArgb(205, 8, 18);

            btnCancel = new Button
            {
                Text      = "취소",
                Location  = new Point(24, 464),
                Size      = new Size(332, 36),
                BackColor = Color.Transparent,
                ForeColor = textGray,
                FlatStyle = FlatStyle.Flat,
                Font      = fontBtnSub,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize         = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = hoverBg;

            // ── 조립 ─────────────────────────────────────────────────
            pnlCard.Controls.AddRange(new Control[]
            {
                lblTitle, lblSubtitle, divider,
                lblIdField,      txtId,
                lblNickField,    txtNick,
                lblPwField,      txtPw,
                lblConfirmField, txtConfirm,
                lblPasswordRule, lblError,
                btnRegister,     btnCancel
            });

            this.Controls.Add(pnlCard);
            this.Controls.Add(pnlDragBar);

            this.ResumeLayout(false);
        }
    }
}
