using System;
using System.Runtime.InteropServices;

static class ActivationTests
{
    [STAThread]
    static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception e)
        {
            Console.WriteLine("Activation failed: type=" + e.GetType().FullName + " hr=" + e.HResult.ToString("X8") + " message=" + e.Message);
            return 1;
        }
    }

    static int Run(string[] args)
    {
        try
        {
            CodexActivation.Activate("AsukaCodex.Nonexistent_0000000000000!App");
            throw new Exception("A missing app must report failure instead of silently launching Explorer.");
        }
        catch (COMException)
        {
            Console.WriteLine("PASS: missing app activation reports an HRESULT failure.");
        }
        if (Array.IndexOf(args, "--real") >= 0)
        {
            uint pid = CodexActivation.Activate("OpenAI.Codex_2p2nqsd0c76g0!App");
            if (pid == 0) throw new Exception("Activation must return a process ID.");
            Console.WriteLine("PASS: actual Codex activation returned pid=" + pid);
        }
        return 0;
    }
}
