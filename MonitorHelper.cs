using System;
using System.Runtime.InteropServices;

namespace SplashAddonPlus
{
    /// <summary>One monitor, in the coordinate space Windows uses for this process.</summary>
    internal sealed class MonitorArea
    {
        public IntPtr Handle { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsPrimary { get; set; }

        /// <summary>The screen without the taskbar.</summary>
        public int WorkLeft { get; set; }
        public int WorkTop { get; set; }
        public int WorkWidth { get; set; }
        public int WorkHeight { get; set; }

        public override string ToString() => $"{Width}x{Height} at {Left},{Top}{(IsPrimary ? " (primary)" : string.Empty)}";
    }

    /// <summary>Read-only monitor information (no monitor is ever chosen: the splash opens where Windows puts it).</summary>
    internal static class MonitorHelper
    {
        public static MonitorArea Primary()
        {
            try
            {
                return Describe(NativeMethods.MonitorFromPoint(new NativeMethods.POINT(), NativeMethods.MONITOR_DEFAULTTOPRIMARY));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The visible rectangle of a shown, not minimized window (x, y, width, height), limited to its monitor, or null.
        /// Windows 10/11 frames have invisible resize borders; the rectangle Windows draws (DWM) is used when it differs
        /// from the window rectangle only by such a border, so the splash lines up with what is on screen.
        /// </summary>
        public static int[] VisibleWindowBounds(IntPtr hWnd)
        {
            try
            {
                if (hWnd == IntPtr.Zero || !NativeMethods.IsWindowVisible(hWnd) || NativeMethods.IsIconic(hWnd)
                    || !NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT r))
                {
                    return null;
                }

                if (NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out NativeMethods.RECT drawn,
                        Marshal.SizeOf(typeof(NativeMethods.RECT))) == 0
                    && IsBorder(drawn.Left - r.Left) && IsBorder(drawn.Top - r.Top) && IsBorder(r.Right - drawn.Right) && IsBorder(r.Bottom - drawn.Bottom))
                {
                    r = drawn;
                }

                MonitorArea monitor = FromWindow(hWnd);
                if (monitor != null)
                {
                    r.Left = Math.Max(r.Left, monitor.Left);
                    r.Top = Math.Max(r.Top, monitor.Top);
                    r.Right = Math.Min(r.Right, monitor.Left + monitor.Width);
                    r.Bottom = Math.Min(r.Bottom, monitor.Top + monitor.Height);
                }

                return r.Right - r.Left >= 200 && r.Bottom - r.Top >= 150
                    ? new[] { r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top }
                    : null;
            }
            catch
            {
                return null;
            }
        }

        // An invisible resize border is a few pixels; a larger gap means the two rectangles use different scales.
        private static bool IsBorder(int gap) => gap >= 0 && gap <= 16;

        /// <summary>Monitor showing most of the window (for a minimized window: where it was before).</summary>
        public static MonitorArea FromWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Describe(NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MONITOR_DEFAULTTONEAREST));
            }
            catch
            {
                return null;
            }
        }

        private static MonitorArea Describe(IntPtr monitor)
        {
            if (monitor == IntPtr.Zero)
            {
                return null;
            }

            var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO)) };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                return null;
            }

            return new MonitorArea
            {
                Handle = monitor,
                Left = info.rcMonitor.Left,
                Top = info.rcMonitor.Top,
                Width = info.rcMonitor.Right - info.rcMonitor.Left,
                Height = info.rcMonitor.Bottom - info.rcMonitor.Top,
                IsPrimary = (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0,
                WorkLeft = info.rcWork.Left,
                WorkTop = info.rcWork.Top,
                WorkWidth = info.rcWork.Right - info.rcWork.Left,
                WorkHeight = info.rcWork.Bottom - info.rcWork.Top
            };
        }

        private static class NativeMethods
        {
            public const uint MONITOR_DEFAULTTOPRIMARY = 1;
            public const uint MONITOR_DEFAULTTONEAREST = 2;
            public const uint MONITORINFOF_PRIMARY = 1;

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MONITORINFO
            {
                public int cbSize;
                public RECT rcMonitor;
                public RECT rcWork;
                public uint dwFlags;
            }

            public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsIconic(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

            [DllImport("dwmapi.dll")]
            public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out RECT value, int size);

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        }
    }
}
