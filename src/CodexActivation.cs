using System;
using System.Runtime.InteropServices;

internal static class CodexActivation
{
    [ComImport, Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments, uint options, out uint processId);
    }

    [DllImport("ole32.dll", PreserveSig = true)]
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out IApplicationActivationManager manager);

    public static uint Activate(string appUserModelId)
    {
        var clsid = new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C");
        var iid = typeof(IApplicationActivationManager).GUID;
        IApplicationActivationManager manager;
        // A short-lived launcher uses a local COM server so activation outlives this process.
        Marshal.ThrowExceptionForHR(CoCreateInstance(ref clsid, IntPtr.Zero, 4, ref iid, out manager));
        try
        {
            uint pid;
            Marshal.ThrowExceptionForHR(manager.ActivateApplication(appUserModelId, null, 2, out pid));
            return pid;
        }
        finally { Marshal.ReleaseComObject(manager); }
    }
}
