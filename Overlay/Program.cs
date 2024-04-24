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

            // ���ؽ� ����
            using (Mutex mutex = new Mutex(true, Process.GetCurrentProcess().ProcessName, out bool createdNew))
            {
                // ���ؽ��� ó�� �����Ǿ����� Ȯ��
                if (createdNew)
                {
                    // ���⿡ ù ��° �ν��Ͻ����� ������ �ڵ带 �ۼ��մϴ�.
                    IntPtr hwnd = GetProcessHandle("HerosLand", "Hero's Land");
                    Application.ThreadException += Application_ThreadException;
                    Application.Run(new MainForm(hwnd));
                    // ���α׷��� ����� �� ���ؽ� ����
                    mutex.ReleaseMutex();
                }
                else
                {
                    MessageBox.Show("�̹� ������");
                    // �̹� �ٸ� �ν��Ͻ��� ���� ���� ���� ������ ���⿡ �ۼ��մϴ�.
                }
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(string.Format("����ġ ���� ����. ������ ����˴ϴ�.\r\n{0}", e.Exception.StackTrace));
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
            return "���� ����";
        }
    }
}