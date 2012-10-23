﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Calendo
{
    /// <summary>
    /// Fixes for maximizing window
    /// Courtesy of LesterLobo
    /// http://blogs.msdn.com/b/llobo/archive/2006/08/01/maximizing-window-_2800_with-windowstyle_3d00_none_2900_-considering-taskbar.aspx
    /// </summary>
    public class FormMaximize
    {
        private const int WM_GETMINMAXINFO = 0x0024;

        /// <summary>
        /// Directly override WinProc messages
        /// </summary>
        public static System.IntPtr WindowProc(
              System.IntPtr handle,
              int message,
              System.IntPtr wParam,
              System.IntPtr lParam,
              ref bool handled)
        {
            switch (message)
            {
                case WM_GETMINMAXINFO: // Directly handle WM_GETMINMAXINFO message
                    WmGetMinMaxInfo(handle, lParam);
                    handled = true;
                    break;
            }

            return (System.IntPtr)0;
        }

        /// <summary>
        /// Get Min-Max screen information from Windows API
        /// </summary>
        /// <param name="handle">Pointer to window</param>
        /// <param name="lParam">Long Parameter</param>
        private static void WmGetMinMaxInfo(System.IntPtr handle, System.IntPtr lParam)
        {
            MinMaxInfo minmaxInfo = (MinMaxInfo)Marshal.PtrToStructure(lParam, typeof(MinMaxInfo));

            // Get current monitor information
            int MONITOR_DEFAULT_TO_NEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(handle, MONITOR_DEFAULT_TO_NEAREST);

            if (monitor != System.IntPtr.Zero) // Not null pointer
            {
                MonitorInfo monitorInfo = new MonitorInfo();
                GetMonitorInfo(monitor, monitorInfo);
                Rectangle rcWorkArea = monitorInfo.WorkArea;
                Rectangle rcMonitorArea = monitorInfo.MonitorArea;
                minmaxInfo.MaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                minmaxInfo.MaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                minmaxInfo.MaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                minmaxInfo.MaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
            }
            Marshal.StructureToPtr(minmaxInfo, lParam, true);
        }

        // Structures below are used by Windows API

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;

            public Point(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MinMaxInfo
        {
            public Point Reserved;
            public Point MaxSize;
            public Point MaxPosition;
            public Point MinTrackSize;
            public Point MaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class MonitorInfo
        {
            public int Size = Marshal.SizeOf(typeof(MonitorInfo));
            public Rectangle MonitorArea = new Rectangle();
            public Rectangle WorkArea = new Rectangle();
            public int Flags = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Windows API external reference
        [DllImport("User32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MonitorInfo lpMonitorInfo);
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
    }
}