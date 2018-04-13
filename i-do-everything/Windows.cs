using System;
using System.Windows.Forms; // just for SendKeys
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace I.Do.Windows
{
    class Windows
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(HandleRef hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr WindowHandle);

        public static void ShowWindow(string procName)
        {
            // maximize Skype app
            foreach (var proc in Process.GetProcessesByName(procName))
            {
                var hWnd = proc.MainWindowHandle;
                ShowWindow(new HandleRef(null, hWnd), 3 /* SW_MAXIMIZE */);
                SetForegroundWindow(hWnd);
                break; // assume first
            }
        }

        public static void SendKey(string key)
        {
            SendKeys.SendWait(key); // https://msdn.microsoft.com/en-us/library/system.windows.forms.sendkeys(v=vs.110).aspx
        }
    }
}
