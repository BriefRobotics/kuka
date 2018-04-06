using System;
using System.Windows.Forms; // just for SendKeys
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Skype
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(HandleRef hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr WindowHandle);

        static void Main(string[] args)
        {
            // maximize Skype app
            foreach (var proc in Process.GetProcessesByName("skype"))
            {
                var hWnd = proc.MainWindowHandle;
                ShowWindow(new HandleRef(null, hWnd), 3 /* SW_MAXIMIZE */);
                SetForegroundWindow(hWnd);
                break; // assume first
            }

            // automate via keystrokes (answer calls, etc.)
            // https://msdn.microsoft.com/en-us/library/system.windows.forms.sendkeys(v=vs.110).aspx
            SendKeys.SendWait("^h"); // Skype Home
        }
    }
}
