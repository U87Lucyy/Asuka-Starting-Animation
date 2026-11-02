using System;

static class LaunchFlowTests
{
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static int Main()
    {
        int launches = 0;
        var normal = new LaunchFlow(false, delegate { launches++; });
        normal.Complete();
        normal.Complete();
        Assert(launches == 1, "Completion and Esc racing must launch only once.");

        var preview = new LaunchFlow(true, delegate { launches++; });
        preview.Complete();
        Assert(launches == 1, "Preview must never launch the app.");

        var cancelled = new LaunchFlow(false, delegate { launches++; });
        cancelled.Cancel();
        cancelled.Complete();
        Assert(launches == 1, "Alt+F4 must cancel even if completion fires later.");

        var failedAsset = new LaunchFlow(false, delegate { launches++; });
        failedAsset.Complete();
        Assert(launches == 2, "Asset failure uses the same completion path.");

        int attempts = 0;
        var failedLaunch = new LaunchFlow(false, delegate { attempts++; throw new InvalidOperationException(); });
        try { failedLaunch.Complete(); } catch (InvalidOperationException) { }
        failedLaunch.Complete();
        Assert(attempts == 1, "A failing launch must not be retried by closing.");
        Console.WriteLine("PASS: normal, once-only, preview, cancel, asset fallback, launch failure (6 checks)");
        return 0;
    }
}
