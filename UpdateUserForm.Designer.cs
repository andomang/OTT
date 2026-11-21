// ============================================================
// UpdateUserForm.Designer.cs — 닉네임 변경 창의 UI 레이아웃 정의
//
// 이 파일이 하는 일:
//   - 창 크기, 색상, 폰트 같은 시각적 설정을 모두 여기서 함
//   - InitializeComponent() 메서드 안에 모든 컨트롤을 만들고 배치함
//   - UpdateUserForm.cs 파일과 partial class로 합쳐져 하나의 클래스가 됨
// ============================================================
using System.Drawing;
using System.Windows.Forms;

namespace PersonalPro
{
    // partial = UpdateUserForm.cs와 이 파일이 합쳐져서 완성되는 클래스
    partial class UpdateUserForm
    {
        // IContainer = 컨트롤들의 생명주기를 관리하는 컨테이너 (메모리 관리용)
        private System.ComponentModel.IContainer components = null;

        // ── 컨트롤 선언 ──────────────────────────────────────────────
        // internal = 같은 프로젝트 안에서 접근 가능
        // Panel = 다른 컨트롤들을 담는 투명/불투명 컨테이너
        internal Panel  pnlDragBar;  // 드래그용 타이틀바 패널
        // Button = 클릭 가능한 버튼
        internal Button btnClose;    // ✕ 닫기 버튼

        // Label = 텍스트를 표시하는 읽기 전용 컨트롤
        internal Label   lblTitle;    // "닉네임 변경" 제목
        internal Label   lblSubtitle; // 부제목

        internal Label   lblNickField; // "새 닉네임" 라벨
        // TextBox = 사용자가 텍스트를 직접 입력하는 컨트롤
        internal TextBox txtNick;      // 닉네임 입력칸

        internal Label   lblError;    // 에러 메시지 표시 라벨 (빨간색)

        internal Button  btnUpdate;   // "수정 완료" 버튼
        internal Button  btnCancel;   // "취소" 버튼

        /// <summary>
        /// 사용된 모든 리소스를 해제합니다 (메모리 정리).
        /// disposing = true면 관리되는 리소스도 해제
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// 창의 모든 컨트롤을 만들고 배치합니다.
        /// 이 메서드는 생성자에서 자동으로 호출됩니다.
        /// </summary>
        private void InitializeComponent()
        {
            // ── 색상 변수 정의 ────────────────────────────────────────
            // Color = 색상 타입. FromArgb(R, G, B)로 RGB 값으로 색상 지정
            Color bgColor   = Color.FromArgb(244, 245, 250);  // 배경색 (연한 회색)
            Color cardColor = Color.White;                     // 카드 배경 (흰색)
            Color inputBg   = Color.FromArgb(247, 248, 252);  // 입력창 배경 (아주 연한 회색)
            Color accentRed = Color.FromArgb(229, 9, 20);     // 강조 빨간색 (버튼, 에러)
            Color textDark  = Color.FromArgb(22, 22, 28);     // 진한 글자색
            Color textGray  = Color.FromArgb(150, 150, 160);  // 회색 글자
            Color textMid   = Color.FromArgb(95, 95, 105);    // 중간 회색 글자
            Color borderCol = Color.FromArgb(226, 227, 234);  // 테두리 색
            Color hoverBg   = Color.FromArgb(243, 244, 248);  // 버튼 마우스 올렸을 때

            // ── 폰트 변수 정의 ────────────────────────────────────────
            // Font = 글꼴 타입. (글꼴이름, 크기, 스타일)
            Font fontTitle  = new Font("Segoe UI", 17f, FontStyle.Bold);  // 제목 폰트 (굵고 큼)
            Font fontSub    = new Font("Segoe UI", 8.5f);                 // 부제목
            Font fontLabel  = new Font("Segoe UI", 8f, FontStyle.Bold);   // 라벨 (작고 굵음)
            Font fontInput  = new Font("Segoe UI", 10f);                  // 입력창
            Font fontBtn    = new Font("Segoe UI", 10.5f, FontStyle.Bold);// 주요 버튼
            Font fontBtnSub = new Font("Segoe UI", 8.5f);                 // 보조 버튼

            // SuspendLayout = 컨트롤 배치 계산을 잠시 멈춤 (한꺼번에 배치해서 성능 향상)
            this.SuspendLayout();

            // ── Form 기본 설정 ────────────────────────────────────────
            this.Text            = "내 정보 수정";           // 창 제목 (작업표시줄에 표시)
            this.ClientSize      = new Size(420, 360);       // 창 안쪽 크기 (픽셀)
            this.MinimumSize     = new Size(420, 360);       // 최소 크기 (리사이즈 방지)
            this.MaximumSize     = new Size(420, 360);       // 최대 크기 (리사이즈 방지)
            this.StartPosition   = FormStartPosition.CenterParent; // 부모 창 중앙에 열기
            this.BackColor       = bgColor;
            this.ForeColor       = textDark;
            this.FormBorderStyle = FormBorderStyle.None;     // 기본 테두리 제거
            this.KeyPreview      = true;  // 키보드 입력을 Form이 먼저 받음 (Enter 처리용)

            // ── 드래그 바 (타이틀바 역할) ─────────────────────────────
            // 이 패널을 드래그하면 창이 이동함
            pnlDragBar = new Panel { Location = new Point(0, 0), Size = new Size(420, 44), BackColor = cardColor };
            // Paint 이벤트 = 그려질 때마다 하단에 구분선을 그음
            pnlDragBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(borderCol), 0, 43, pnlDragBar.Width, 43);

            // ✕ 닫기 버튼
            btnClose = new Button
            {
                Text      = "✕",
                Location  = new Point(376, 7),  // 오른쪽 끝에 위치
                Size      = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = textMid,
                BackColor = cardColor,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand   // 마우스 올리면 손가락 모양
            };
            btnClose.FlatAppearance.BorderSize = 0;
            // 마우스 올리면 빨간 배경, 벗어나면 원래 색으로
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = accentRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = cardColor; btnClose.ForeColor = textMid; };

            // 타이틀바에 텍스트 라벨과 닫기 버튼 추가
            pnlDragBar.Controls.Add(new Label
            {
                Text = "OTT Multi-View — 내 정보 수정",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textGray,
                Location  = new Point(14, 13),
                AutoSize  = true
            });
            pnlDragBar.Controls.Add(btnClose);

            // ── 카드 패널 (실제 내용이 들어가는 흰색 박스) ────────────
            // Location = 카드의 위치: 왼쪽 20px, 위 52px
            Panel pnlCard = new Panel { Location = new Point(20, 52), Size = new Size(380, 296), BackColor = cardColor };

            // 큰 제목 "닉네임 변경"
            lblTitle = new Label
            {
                Text     = "닉네임 변경",
                Font     = fontTitle,
                ForeColor = textDark,
                Location = new Point(24, 24),
                Size     = new Size(332, 36),
                AutoSize = false
            };

            // 부제목 "사용할 닉네임을 입력하세요"
            lblSubtitle = new Label
            {
                Text      = "사용할 닉네임을 입력하세요",
                Font      = fontSub,
                ForeColor = textMid,
                Location  = new Point(24, 62),
                AutoSize  = true
            };

            // 제목과 내용 사이 가로 구분선 (높이 1px)
            Panel divider = new Panel { Location = new Point(24, 86), Size = new Size(332, 1), BackColor = borderCol };

            // "새 닉네임" 입력 필드 라벨
            lblNickField = new Label
            {
                Text = "새 닉네임", Font = fontLabel, ForeColor = textMid,
                Location = new Point(24, 102), AutoSize = true
            };

            // 닉네임 입력칸 (ForeColor = textDark로 설정 — 흰 배경에서 잘 보이게)
            txtNick = new TextBox
            {
                Location    = new Point(24, 120),
                Size        = new Size(332, 36),
                BackColor   = inputBg,
                ForeColor   = textDark,       // 진한 색으로 입력 텍스트가 잘 보임
                BorderStyle = BorderStyle.FixedSingle,
                Font        = fontInput
            };

            // 에러 메시지 라벨 (빨간색, 처음엔 빈 문자열)
            lblError = new Label
            {
                Text = "", Font = new Font("Segoe UI", 8.5f), ForeColor = accentRed,
                Location = new Point(24, 166), Size = new Size(332, 18), AutoSize = false
            };

            // "수정 완료" 버튼 (빨간 배경)
            btnUpdate = new Button
            {
                Text      = "수정 완료",
                Location  = new Point(24, 194),
                Size      = new Size(332, 50),
                BackColor = accentRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = fontBtn,
                Cursor    = Cursors.Hand
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            // 마우스 올리면 조금 더 어두운 빨간색
            btnUpdate.FlatAppearance.MouseOverBackColor = Color.FromArgb(205, 8, 18);

            // "취소" 버튼 (투명 배경, 회색 글자)
            btnCancel = new Button
            {
                Text      = "취소",
                Location  = new Point(24, 252),
                Size      = new Size(332, 36),
                BackColor = Color.Transparent,
                ForeColor = textGray,
                FlatStyle = FlatStyle.Flat,
                Font      = fontBtnSub,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize         = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = hoverBg;

            // 카드 안에 컨트롤들 한꺼번에 추가
            // AddRange = 여러 컨트롤을 한 번에 추가 (Control[] = 컨트롤 배열)
            pnlCard.Controls.AddRange(new Control[]
            {
                lblTitle, lblSubtitle, divider,
                lblNickField, txtNick,
                lblError, btnUpdate, btnCancel
            });

            // Form에 카드와 타이틀바 추가 (나중에 추가한 것이 더 앞에 렌더링됨)
            this.Controls.Add(pnlCard);
            this.Controls.Add(pnlDragBar);

            // ResumeLayout = 잠시 멈췄던 배치 계산 재개
            this.ResumeLayout(false);
        }
    }
}
