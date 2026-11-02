using System;

static class WindowTargetTests
{
    static int Main()
    {
        WindowTarget target = WindowTarget.FindCodex();
        if (target == null) throw new Exception("The running Codex window must be found by its installed application identity.");
        var bounds = target.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new Exception("Codex must have positive physical window bounds.");
        Console.WriteLine("PASS: Codex hwnd=" + target.Handle + " bounds=" + bounds);
        return 0;
    }
}
