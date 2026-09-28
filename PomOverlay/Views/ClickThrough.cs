using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PomOverlay
{
    // マウス操作を下のウィンドウへ透過させる（レイヤードウィンドウ + WS_EX_TRANSPARENT）
    internal static class ClickThrough
    {
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        const int GWL_EXSTYLE = -20, WS_EX_TRANSPARENT = 0x20, WS_EX_LAYERED = 0x80000;

        public static void Apply(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }
    }
}
