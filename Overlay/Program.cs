using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Overlay
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // 뮤텍스 생성
            using (Mutex mutex = new Mutex(true, Process.GetCurrentProcess().ProcessName, out bool createdNew))
            {
                // 뮤텍스가 처음 생성되었는지 확인
                if (createdNew)
                {
                    // 여기에 첫 번째 인스턴스에서 실행할 코드를 작성합니다.
                    IntPtr hwnd = GetProcessHandle("HerosLand", "Hero's Land");
                    Application.ThreadException += Application_ThreadException;
                    Application.Run(new MainForm(hwnd));
                    // 프로그램이 종료될 때 뮤텍스 해제
                    mutex.ReleaseMutex();
                }
                else
                {
                    MessageBox.Show("이미 실행중");
                    // 이미 다른 인스턴스가 실행 중일 때의 동작을 여기에 작성합니다.
                }
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(string.Format("예상치 못한 오류. 강제로 종료됩니다.\r\n{0}", e.Exception.StackTrace));
            Process.GetCurrentProcess().Kill();
        }

        static IntPtr GetProcessHandle(string processName, string windowTitle)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                IntPtr mainWindowHandle = process.MainWindowHandle;
                if (mainWindowHandle != IntPtr.Zero)
                {
                    if (GetWindowTitle(process.MainWindowHandle) == windowTitle)
                    {
                        return process.MainWindowHandle;
                    }
                }
            }
            return IntPtr.Zero;
        }

        static string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            System.Text.StringBuilder title = new System.Text.StringBuilder(nChars);
            if (GetWindowText(hWnd, title, nChars) > 0)
            {
                return title.ToString();
            }
            return "제목 없음";
        }
    }
}