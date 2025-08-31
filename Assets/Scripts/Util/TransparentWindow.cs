using System;
using System.Runtime.InteropServices;
using UnityEngine;


namespace LunaLyrics.Util
{
    public class TransparentWindow : MonoBehaviour
    {
        [DllImport("user32.dll")]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cxTopHeight;
            public int cxBottomHeight;
        }

        [DllImport("Dwmapi.dll")]
        private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

        const int GWL_EXSTYLE = -20;

        const uint WS_EX_LAYERED = 0x00080000;
        const uint WS_EX_TRANSPARENT = 0x00000020;

        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;

        static readonly IntPtr HWND_TOPMOST = new(-1);


        private void Start()
        {
#if !UNITY_EDITOR
        IntPtr hWnd = GetActiveWindow();

        MARGINS margins = new() { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);

        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
#endif

        }
    }
}