// ============================================================
// UpdateUserForm.cs — 닉네임 변경 폼
//
// 이 창이 하는 일:
//   - 현재 닉네임을 보여주고 새 닉네임을 입력받음
//   - 서버에 닉네임 변경을 요청함
//   - 성공하면 메인 창의 닉네임 표시도 즉시 바뀜
// ============================================================
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersonalPro
{
    // partial class = 같은 클래스를 여러 파일에 나눠서 작성 가능
    // UpdateUserForm.Designer.cs 파일과 합쳐져서 완성됨
    public partial class UpdateUserForm : Form
    {
        // ── 필드 ─────────────────────────────────────────────────────
        // int = 정수 타입. 로그인한 사용자의 DB 번호
        // readonly = 처음 설정 후 바꿀 수 없음 (실수로 수정 방지)
        private readonly int userIdx;

        // ── Windows API (드래그 이동용) ───────────────────────────────
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int  SendMessage(System.IntPtr hWnd, int msg, int wp, int lp);

        // ── 생성자 ────────────────────────────────────────────────────
        /// <summary>
        /// 닉네임 변경 창을 만듭니다.
        /// </summary>
        /// <param name="uIdx">사용자 DB 번호 (int)</param>
        /// <param name="currentNick">현재 닉네임 — 입력창에 미리 채워줌 (string)</param>
        public UpdateUserForm(int uIdx, string currentNick)
        {
            userIdx = uIdx;
            InitializeComponent();  // Designer.cs에서 만든 UI 컨트롤 초기화

            // 현재 닉네임을 입력창에 미리 표시
            txtNick.Text      = currentNick;
            // 글자 색을 진한 색으로 설정 (플레이스홀더와 구별)
            txtNick.ForeColor = Color.FromArgb(22, 22, 28);

            WireEvents();  // 버튼 클릭 등 이벤트 연결
        }

        // ── 이벤트 연결 ──────────────────────────────────────────────

        /// <summary>
        /// UI 이벤트들(클릭, 마우스다운 등)을 해당 메서드에 연결합니다.
        /// </summary>
        private void WireEvents()
        {
            // 타이틀바 패널을 드래그하면 창이 이동
            pnlDragBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); }
            };

            // ✕ 닫기 버튼, 취소 버튼 → 창 닫기
            btnClose.Click  += (s, e) => this.Close();
            btnCancel.Click += (s, e) => this.Close();

            // 수정 완료 버튼 클릭 → 비동기 업데이트 실행
            btnUpdate.Click += async (s, e) => await UpdateAsync();
            // Enter 키 → 수정 완료 버튼과 동일한 동작
            this.KeyDown    += async (s, e) => { if (e.KeyCode == Keys.Enter) await UpdateAsync(); };
        }

        // ── 닉네임 변경 처리 ──────────────────────────────────────────

        /// <summary>
        /// 입력된 닉네임의 유효성을 검사하고 서버에 변경 요청을 보냅니다.
        /// async Task = 비동기 메서드 — 서버 응답 기다리는 동안 화면이 멈추지 않음
        /// </summary>
        private async Task UpdateAsync()
        {
            // 이미 처리 중이면 중복 실행 방지
            if (!btnUpdate.Enabled) return;

            // Trim() = 앞뒤 공백 제거
            string nick = txtNick.Text.Trim();

            // 닉네임이 비어있으면 에러 표시 후 중단
            if (string.IsNullOrEmpty(nick)) { ShowError("닉네임을 입력해주세요."); return; }

            ShowError("");        // 에러 메시지 지우기
            SetLoading(true);     // 버튼 비활성화 + "처리 중..." 표시

            // Task.Run = 서버 요청을 백그라운드에서 실행 (UI 스레드 안 막음)
            // bool = 성공(true) / 실패(false)
            bool success = await Task.Run(() => DatabaseHelper.UpdateNickname(userIdx, nick));

            if (success)
            {
                MessageBox.Show("닉네임이 변경되었습니다.", "수정 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // DialogResult.OK = 이 창을 열었던 쪽에 "성공했음"을 알림
                // MainForm에서 이 값을 확인해서 닉네임 표시를 갱신함
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                ShowError("서버 오류가 발생했습니다. 다시 시도해주세요.");
                SetLoading(false);  // 버튼 다시 활성화
            }
        }

        // ── 헬퍼 메서드 ──────────────────────────────────────────────

        /// <summary>
        /// 에러 라벨에 메시지를 표시합니다. 빈 문자열을 넣으면 메시지가 사라집니다.
        /// </summary>
        /// <param name="message">표시할 에러 텍스트 (string)</param>
        private void ShowError(string message) => lblError.Text = message;

        /// <summary>
        /// 로딩 중 여부에 따라 버튼 상태를 전환합니다.
        /// </summary>
        /// <param name="loading">true = 로딩 중 (버튼 비활성), false = 완료 (버튼 활성) (bool)</param>
        private void SetLoading(bool loading)
        {
            btnUpdate.Enabled = !loading;                         // 비활성/활성 전환
            btnUpdate.Text    = loading ? "처리 중..." : "수정 완료"; // 텍스트 전환
        }
    }
}
