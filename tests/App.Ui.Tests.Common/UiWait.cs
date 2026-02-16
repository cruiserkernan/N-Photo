namespace App.Ui.Tests.Common;

public static class UiWait
{
    public static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, string timeoutMessage)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(timeoutMessage);
    }

    public static Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        return WaitForConditionAsync(
            () => File.Exists(path) && new FileInfo(path).Length > 0,
            timeout,
            $"Timed out waiting for screenshot file: {path}");
    }
}
