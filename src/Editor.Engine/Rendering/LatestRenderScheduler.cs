using System.Threading;
using Editor.Engine.Abstractions.Rendering;

namespace Editor.Engine.Rendering;

public sealed class LatestRenderScheduler : IRenderScheduler
{
    private CancellationTokenSource? _activeCts;

    public void ScheduleLatest(Func<CancellationToken, Task> work)
    {
        var nextCts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _activeCts, nextCts);
        previous?.Cancel();
        previous?.Dispose();

        _ = RunAsync(work, nextCts.Token);
    }

    public void Cancel()
    {
        var current = Interlocked.Exchange(ref _activeCts, null);
        current?.Cancel();
        current?.Dispose();
    }

    public void Dispose()
    {
        Cancel();
    }

    private static async Task RunAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        try
        {
            await work(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Intentionally ignored. Newer render request replaced this one.
        }
    }
}
