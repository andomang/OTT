// ============================================================
// ChangePasswordForm.cs — 비밀번호 변경 폼
//
// 이 창이 하는 일:
//   - 새 비밀번호를 입력받아 서버에 변경 요청을 보냄
//   - 비밀번호 규칙 검사: 영문 + 숫자 + 특수문자 포함 8자 이상
//   - 비밀번호 확인칸을 통해 오타 방지
// ============================================================
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersonalPro
{
    // partial class = ChangePasswordForm.Designer.cs와 합쳐지는 클래스
    public partial class ChangePasswordForm : Form
    {
        // ── 필드 ─────────────────────────────────────────────────────
        // int = 정수. 서버에서 어떤 사용자의 비밀번호를 바꿀지 식별하는 번호
        private readonly int userIdx;

        // Regex = 정규식(Regular Expression) — 문자열의 패턴을 검사하는 도구
        // 비밀번호 규칙: 영문 + 숫자 + 특수문자(@$!%*#?&) 포함, 최소 8자
        // RegexOptions.Compiled = 미리 컴파일해서 빠르게 동작하게 함
        private static readonly Regex PasswordPattern = new(
            @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$",
            RegexOptions.Compiled);

        // ── Windows API (드래그 이동용) ───────────────────────────────
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int  SendMessage(System.IntPtr hWnd, int msg, int wp, int lp);

        // ── 생성자 ────────────────────────────────────────────────────
        /// <summary>
        /// 비밀번호 변경 창을 만듭니다.
        /// </summary>
        /// <param name="uIdx">사용자 DB 번호 (int)</param>
        public ChangePasswordForm(int uIdx)
        {
            userIdx = uIdx;
            InitializeComponent();  // Designer.cs에서 만든 UI 초기화
            WireEvents();           // 이벤트 핸들러 연결
        }

        // ── 이벤트 연결 ──────────────────────────────────────────────

        /// <summary>
        /// 버튼 클릭, 드래그 등 이벤트를 각 메서드에 연결합니다.
        /// </summary>
        private void WireEvents()
        {
            // 타이틀바 드래그 → 창 이동
            pnlDragBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); }
            };

            // 닫기 버튼, 취소 버튼 → 창 닫기
            btnClose.Click  += (s, e) => this.Close();
            btnCancel.Click += (s, e) => this.Close();

            // 비밀번호 입력창에 플레이스홀더 + 마스킹 동작 적용
            AttachPasswordPlaceholder(txtPw);
            AttachPasswordPlaceholder(txtConfirm);

            // 변경 완료 버튼 클릭, Enter 키 → 비동기 업데이트 실행
            btnUpdate.Click += async (s, e) => await UpdateAsync();
            this.KeyDown    += async (s, e) => { if (e.KeyCode == Keys.Enter) await UpdateAsync(); };
        }

        // ── 플레이스홀더 동작 ─────────────────────────────────────────

        /// <summary>
        /// 비밀번호 입력창에 플레이스홀더와 마스킹 전환을 적용합니다.
        ///
        /// 동작 방식:
        ///   - 클릭하면 (Enter 이벤트): 플레이스홀더 텍스트 제거 + 비밀번호 마스킹(●●●) 켜기
        ///   - 포커스 잃으면 (Leave 이벤트): 내용이 비어있으면 플레이스홀더 다시 표시
        /// </summary>
        /// <param name="tb">적용할 TextBox 컨트롤</param>
        private static void AttachPasswordPlaceholder(TextBox tb)
        {
            tb.Enter += (s, e) =>
            {
                // Tag 속성에 저장된 플레이스홀더 텍스트와 같으면 비우고 마스킹 켜기
                if (tb.Text == tb.Tag?.ToString())
                {
                    tb.Text                  = "";
                    tb.ForeColor             = Color.FromArgb(22, 22, 28);  // 진한 글자색
                    tb.UseSystemPasswordChar = true;  // 입력 내용을 ● 로 마스킹
                }
            };
            tb.Leave += (s, e) =>
            {
                // 아무것도 입력 안 했으면 플레이스홀더 다시 표시
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.UseSystemPasswordChar = false;               // 마스킹 해제
                    tb.Text      = tb.Tag?.ToString() ?? "";        // 플레이스홀더 복원
                    tb.ForeColor = Color.FromArgb(150, 150, 160);  // 연한 회색
                }
            };
        }

        // ── 비밀번호 변경 처리 ────────────────────────────────────────

        /// <summary>
        /// 새 비밀번호를 검증하고 서버에 변경 요청을 보냅니다.
        /// async Task = 비동기 메서드 (서버 응답 기다리는 동안 화면 안 멈춤)
        /// </summary>
        private async Task UpdateAsync()
        {
            // 이미 처리 중이면 중복 실행 방지
            if (!btnUpdate.Enabled) return;

            // 입력값 가져오기 (플레이스홀더와 같으면 빈 문자열로 처리)
            string pw      = GetValue(txtPw);
            string confirm = GetValue(txtConfirm);

            // ── 유효성 검사 ───────────────────────────────────────────
            if (string.IsNullOrEmpty(pw))     { ShowError("새 비밀번호를 입력해주세요."); return; }
            if (pw != confirm)                { ShowError("비밀번호가 일치하지 않습니다."); return; }
            if (!PasswordPattern.IsMatch(pw)) { ShowError("영문·숫자·특수문자 포함 8자 이상이어야 합니다."); return; }

            ShowError("");       // 에러 메시지 지우기
            SetLoading(true);    // 버튼 비활성화

            // Task.Run = 서버 요청을 백그라운드 스레드에서 실행 (UI 안 멈춤)
            // bool success = 성공(true) / 실패(false)
            bool success = await Task.Run(() => DatabaseHelper.UpdatePassword(userIdx, pw));

            if (success)
            {
                MessageBox.Show("비밀번호가 변경되었습니다.", "변경 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                ShowError("서버 오류가 발생했습니다. 다시 시도해주세요.");
                SetLoading(false);
            }
        }

        // ── 헬퍼 메서드 ──────────────────────────────────────────────

        /// <summary>
        /// TextBox의 값을 가져옵니다.
        /// 플레이스홀더(Tag 값)와 같으면 빈 문자열을 반환합니다.
        /// </summary>
        /// <param name="tb">값을 가져올 TextBox</param>
        /// <returns>실제 입력값 (string). 플레이스홀더면 "" 빈 문자열</returns>
        private static string GetValue(TextBox tb)
        {
            string val = tb.Text.Trim();
            return val == tb.Tag?.ToString() ? "" : val;
        }

        /// <summary>
        /// 에러 라벨에 메시지를 표시합니다. 빈 문자열이면 지워집니다.
        /// </summary>
        /// <param name="message">표시할 에러 텍스트 (string)</param>
        private void ShowError(string message) => lblError.Text = message;

        /// <summary>
        /// 로딩 상태에 따라 버튼 상태를 전환합니다.
        /// </summary>
        /// <param name="loading">true = 처리 중 / false = 완료 (bool)</param>
        private void SetLoading(bool loading)
        {
            btnUpdate.Enabled = !loading;
            btnUpdate.Text    = loading ? "처리 중..." : "변경 완료";
        }
    }
}
