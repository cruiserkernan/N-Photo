namespace Editor.Engine.Abstractions.Rendering;

public interface IRenderScheduler : IDisposable
{
    void ScheduleLatest(Func<CancellationToken, Task> work);

    void Cancel();
}
