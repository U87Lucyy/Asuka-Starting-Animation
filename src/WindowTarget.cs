using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

internal sealed class WindowTarget
{
    public readonly IntPtr Handle;

    private WindowTarget(IntPtr handle) { Handle = handle; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    private delegate bool EnumWindow(IntPtr handle, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint process);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr handle, int attribute, out NativeRect rect, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int width, int height, uint flags);

    public Rectangle Bounds
    {
        get { return GetBounds(Handle); }
    }

    public static Rectangle GetBounds(IntPtr handle)
    {
        NativeRect rect;
        if (DwmGetWindowAttribute(handle, 9, out rect, Marshal.SizeOf(typeof(NativeRect))) != 0)
            if (!GetWindowRect(handle, out rect)) return Rectangle.Empty;
        return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public void Align(IntPtr animation)
    {
        Rectangle rect = Bounds;
        if (rect.IsEmpty) return;
        // Both rectangles use physical pixels, including at mixed monitor DPI.
        SetWindowPos(animation, IntPtr.Zero, rect.X, rect.Y, rect.Width, rect.Height, 0x0014);
        int corners = 2;
        DwmSetWindowAttribute(animation, 33, ref corners, sizeof(int));
    }

    public static WindowTarget FindCodex()
    {
        WindowTarget found = null;
        EnumWindows(delegate(IntPtr handle, IntPtr parameter)
        {
            if (!IsWindowVisible(handle) || IsIconic(handle)) return true;
            uint pid;
            GetWindowThreadProcessId(handle, out pid);
            try
            {
                using (Process process = Process.GetProcessById((int)pid))
                {
                    if (!process.ProcessName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase)) return true;
                    string path = process.MainModule.FileName;
                    if (path.IndexOf("\\WindowsApps\\OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) < 0) return true;
                }
                Rectangle bounds = GetBounds(handle);
                if (bounds.Width < 400 || bounds.Height < 300) return true;
                found = new WindowTarget(handle);
                return false;
            }
            catch (Win32Exception) { return true; }
            catch (InvalidOperationException) { return true; }
            catch (ArgumentException) { return true; }
        }, IntPtr.Zero);
        return found;
    }
}
