namespace App.Ui.Tests.Common;

public static class DesktopUiTestGate
{
    public const string EnableDesktopUiTestsEnvironmentVariable = "NPHOTO_ENABLE_DESKTOP_UI_TESTS";

    public static bool TryGetSkipReason(out string reason)
    {
        if (!OperatingSystem.IsWindows())
        {
            reason = "Desktop UI automation tests run on Windows only.";
            return true;
        }

        var enabled = Environment.GetEnvironmentVariable(EnableDesktopUiTestsEnvironmentVariable);
        if (!string.Equals(enabled, "1", StringComparison.Ordinal))
        {
            reason = $"Set {EnableDesktopUiTestsEnvironmentVariable}=1 to enable desktop UI tests.";
            return true;
        }

        reason = string.Empty;
        return false;
    }
}
