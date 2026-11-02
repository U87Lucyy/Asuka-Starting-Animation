using System;

internal sealed class LaunchFlow
{
    private readonly bool preview;
    private readonly Action launch;
    private bool finished;

    public LaunchFlow(bool preview, Action launch)
    {
        this.preview = preview;
        this.launch = launch;
    }

    public void Complete()
    {
        if (finished) return;
        finished = true;
        if (!preview) launch();
    }

    public void Cancel()
    {
        finished = true;
    }
}
