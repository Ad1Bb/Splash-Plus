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

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        }
    }
}
