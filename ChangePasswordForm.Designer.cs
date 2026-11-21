// ============================================================
// ChangePasswordForm.Designer.cs — 비밀번호 변경 창의 UI 레이아웃 정의
//
// 이 파일이 하는 일:
//   - 창에 들어가는 모든 컨트롤(버튼, 입력창 등)을 만들고 배치
//   - ChangePasswordForm.cs와 partial class로 합쳐져 하나의 클래스가 됨
// ============================================================
using System.Drawing;
using System.Windows.Forms;

namespace PersonalPro
{
    // partial = ChangePasswordForm.cs와 이 파일이 합쳐지는 클래스
    partial class ChangePasswordForm
    {
        // IContainer = 컨트롤들의 생명주기를 관리하는 컨테이너
        private System.ComponentModel.IContainer components = null;

        // ── 컨트롤 선언 ──────────────────────────────────────────────
        // internal = 같은 프로젝트 내에서 접근 가능
        internal Panel  pnlDragBar;  // 드래그로 창 이동하는 타이틀바 역할 패널
        internal Button btnClose;    // ✕ 닫기 버튼

        internal Label   lblTitle;    // 큰 제목 "비밀번호 변경"
        internal Label   lblSubtitle; // 부제목

        internal Label   lblPwField;     // "새 비밀번호" 라벨
        internal TextBox txtPw;          // 새 비밀번호 입력칸

        internal Label   lblConfirmField; // "비밀번호 확인" 라벨
        internal TextBox txtConfirm;      // 비밀번호 재입력 확인칸

        internal Label   lblPasswordRule; // 비밀번호 규칙 안내 텍스트
        internal Label   lblError;        // 에러 메시지 라벨 (빨간색)

        internal Button  btnUpdate;  // "변경 완료" 버튼
        internal Button  btnCancel;  // "취소" 버튼

        /// <summary>
        /// 사용된 모든 리소스를 해제합니다 (메모리 정리).
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// 창의 모든 컨트롤을 만들고 배치합니다.
        /// 생성자에서 자동으로 호출됩니다.
        /// </summary>
        private void InitializeComponent()
        {
            // ── 색상 팔레트 ───────────────────────────────────────────
            Color bgColor   = Color.FromArgb(244, 245, 250);  // 창 배경 (연한 회색)
            Color cardColor = Color.White;                     // 카드 배경 (흰색)
            Color inputBg   = Color.FromArgb(247, 248, 252);  // 입력창 배경
            Color accentRed = Color.FromArgb(229, 9, 20);     // 강조 빨간색
            Color textDark  = Color.FromArgb(22, 22, 28);     // 진한 글자색
            Color textGray  = Color.FromArgb(150, 150, 160);  // 연한 회색 글자
            Color textMid   = Color.FromArgb(95, 95, 105);    // 중간 회색 글자
            Color borderCol = Color.FromArgb(226, 227, 234);  // 테두리 색
            Color hoverBg   = Color.FromArgb(243, 244, 248);  // 마우스 올렸을 때 배경

            // ── 폰트 ─────────────────────────────────────────────────
            Font fontTitle  = new Font("Segoe UI", 17f, FontStyle.Bold);   // 큰 제목
            Font fontSub    = new Font("Segoe UI", 8.5f);                  // 부제목
            Font fontLabel  = new Font("Segoe UI", 8f, FontStyle.Bold);    // 필드 라벨
            Font fontInput  = new Font("Segoe UI", 10f);                   // 입력창 텍스트
            Font fontBtn    = new Font("Segoe UI", 10.5f, FontStyle.Bold); // 주요 버튼
            Font fontBtnSub = new Font("Segoe UI", 8.5f);                  // 보조 버튼
            Font fontRule   = new Font("Segoe UI", 7.5f);                  // 규칙 안내 (작은 글씨)

            this.SuspendLayout();  // 배치 계산 잠시 중단 (성능 향상)

            // ── Form 기본 설정 ────────────────────────────────────────
            this.Text            = "비밀번호 변경";
            this.ClientSize      = new Size(420, 460);       // 창 안쪽 크기
            this.MinimumSize     = new Size(420, 460);       // 고정 크기 (최소=최대)
            this.MaximumSize     = new Size(420, 460);
            this.StartPosition   = FormStartPosition.CenterParent; // 부모 창 가운데
            this.BackColor       = bgColor;
            this.ForeColor       = textDark;
            this.FormBorderStyle = FormBorderStyle.None;     // 기본 테두리 제거
            this.KeyPreview      = true;  // Enter 키를 Form이 먼저 받음

            // ── 드래그 바 (44px 높이, 창 최상단) ─────────────────────
            pnlDragBar = new Panel { Location = new Point(0, 0), Size = new Size(420, 44), BackColor = cardColor };
            // Paint = 다시 그려질 때마다 하단 구분선 그리기
            pnlDragBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(borderCol), 0, 43, pnlDragBar.Width, 43);

            // ✕ 닫기 버튼
            btnClose = new Button
            {
                Text = "✕", Location = new Point(376, 7), Size = new Size(36, 30),
                FlatStyle = FlatStyle.Flat, ForeColor = textMid, BackColor = cardColor,
                Font = new Font("Segoe UI", 9f), Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            // hover 시 빨간색으로 변경
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = accentRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = cardColor; btnClose.ForeColor = textMid; };

            // 타이틀바 상단 텍스트 + 닫기 버튼 추가
            pnlDragBar.Controls.Add(new Label
            {
                Text = "OTT Multi-View — 비밀번호 변경", Font = new Font("Segoe UI", 8.5f),
                ForeColor = textGray, Location = new Point(14, 13), AutoSize = true
            });
            pnlDragBar.Controls.Add(btnClose);

            // ── 카드 패널 (실제 입력 폼이 들어가는 흰 박스) ──────────
            // Location = 카드 시작 위치 (좌:20px, 상:52px)
            // Size = 카드 크기 (380×396)
            Panel pnlCard = new Panel { Location = new Point(20, 52), Size = new Size(380, 396), BackColor = cardColor };

            // 제목 라벨 "비밀번호 변경"
            lblTitle = new Label
            {
                Text = "비밀번호 변경", Font = fontTitle, ForeColor = textDark,
                Location = new Point(24, 24), Size = new Size(332, 36), AutoSize = false
            };
            // 부제목 라벨
            lblSubtitle = new Label
            {
                Text = "새 비밀번호를 입력하세요", Font = fontSub, ForeColor = textMid,
                Location = new Point(24, 62), AutoSize = true
            };
            // 제목과 내용 사이 1px 가로 구분선
            Panel divider = new Panel { Location = new Point(24, 86), Size = new Size(332, 1), BackColor = borderCol };

            // ── 새 비밀번호 입력 ──────────────────────────────────────
            // y=102: 구분선(86px) 아래 16px
            lblPwField = new Label
            {
                Text = "새 비밀번호", Font = fontLabel, ForeColor = textMid,
                Location = new Point(24, 102), AutoSize = true
            };
            // Tag = 플레이스홀더 텍스트 (포커스 없을 때 표시)
            txtPw = new TextBox
            {
                Location = new Point(24, 120), Size = new Size(332, 36),
                BackColor = inputBg, ForeColor = textGray,
                BorderStyle = BorderStyle.FixedSingle, Font = fontInput,
                Text = "8자 이상 + 특수문자", Tag = "8자 이상 + 특수문자"
            };

            // ── 비밀번호 확인 입력 ────────────────────────────────────
            lblConfirmField = new Label
            {
                Text = "비밀번호 확인", Font = fontLabel, ForeColor = textMid,
                Location = new Point(24, 168), AutoSize = true
            };
            txtConfirm = new TextBox
            {
                Location = new Point(24, 186), Size = new Size(332, 36),
                BackColor = inputBg, ForeColor = textGray,
                BorderStyle = BorderStyle.FixedSingle, Font = fontInput,
                Text = "비밀번호를 다시 입력하세요", Tag = "비밀번호를 다시 입력하세요"
            };

            // 비밀번호 규칙 안내 (작은 회색 글씨)
            lblPasswordRule = new Label
            {
                Text = "영문·숫자·특수문자(@$!%*#?&) 포함, 최소 8자",
                Font = fontRule, ForeColor = Color.FromArgb(180, 182, 190),
                Location = new Point(24, 230), Size = new Size(332, 16), AutoSize = false
            };

            // 에러 메시지 라벨 (빨간색, 기본 빈 문자열)
            lblError = new Label
            {
                Text = "", Font = new Font("Segoe UI", 8.5f), ForeColor = accentRed,
                Location = new Point(24, 250), Size = new Size(332, 18), AutoSize = false
            };

            // "변경 완료" 버튼 (빨간 배경)
            btnUpdate = new Button
            {
                Text = "변경 완료", Location = new Point(24, 278), Size = new Size(332, 50),
                BackColor = accentRed, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = fontBtn, Cursor = Cursors.Hand
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            // hover 시 조금 더 어두운 빨간색
            btnUpdate.FlatAppearance.MouseOverBackColor = Color.FromArgb(205, 8, 18);

            // "취소" 버튼 (투명 배경)
            btnCancel = new Button
            {
                Text = "취소", Location = new Point(24, 336), Size = new Size(332, 36),
                BackColor = Color.Transparent, ForeColor = textGray,
                FlatStyle = FlatStyle.Flat, Font = fontBtnSub, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize         = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = hoverBg;

            // 카드에 모든 컨트롤 한꺼번에 추가
            pnlCard.Controls.AddRange(new Control[]
            {
                lblTitle, lblSubtitle, divider,
                lblPwField, txtPw,
                lblConfirmField, txtConfirm,
                lblPasswordRule, lblError,
                btnUpdate, btnCancel
            });

            // Form에 카드와 타이틀바 추가
            this.Controls.Add(pnlCard);
            this.Controls.Add(pnlDragBar);
            this.ResumeLayout(false);  // 배치 계산 재개
        }
    }
}
