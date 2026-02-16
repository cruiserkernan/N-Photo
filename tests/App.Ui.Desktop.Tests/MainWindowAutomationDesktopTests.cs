using System.Diagnostics;
using App.Ui.Tests.Common;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace App.Ui.Desktop.Tests;

public class MainWindowAutomationDesktopTests
{
    [DesktopUiFact]
    public async Task StartupAddTransformScenario_WritesScreenshot_AndSetsStatus()
    {
        var screenshotPath = UiArtifactPaths.PrepareScreenshotPath("desktop", "startup-add-transform");
        var startInfo = CreateAppStartInfo(screenshotPath);
        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Failed to launch app process for desktop automation.");

        try
        {
            using var automation = new UIA3Automation();
            var window = await WaitForMainWindowAsync(process, automation, TimeSpan.FromSeconds(20));
            var statusElement = await WaitForStatusElementAsync(window, automation, TimeSpan.FromSeconds(20));

            await UiWait.WaitForConditionAsync(
                () => statusElement.Name.Contains("Transform", StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(10),
                "Timed out waiting for transform status text in desktop UI.");

            await UiWait.WaitForFileAsync(screenshotPath, TimeSpan.FromSeconds(20));
            PngAssertions.AssertValid(screenshotPath);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static ProcessStartInfo CreateAppStartInfo(string screenshotPath)
    {
        var root = UiArtifactPaths.RepositoryRoot;
        var appHostPath = Path.Combine(root, "src", "App", "bin", "Debug", "net10.0", "App.exe");
        var appDllPath = Path.Combine(root, "src", "App", "bin", "Debug", "net10.0", "App.dll");

        ProcessStartInfo startInfo;
        if (File.Exists(appHostPath))
        {
            startInfo = new ProcessStartInfo(appHostPath);
        }
        else if (File.Exists(appDllPath))
        {
            startInfo = new ProcessStartInfo("dotnet", $"\"{appDllPath}\"");
        }
        else
        {
            throw new FileNotFoundException($"Could not locate app build output at '{appHostPath}' or '{appDllPath}'.");
        }

        startInfo.WorkingDirectory = root;
        startInfo.UseShellExecute = false;
        startInfo.EnvironmentVariables["NPHOTO_AUTOMATION_MODE"] = "1";
        startInfo.EnvironmentVariables["NPHOTO_AUTOMATION_SCENARIO"] = "startup-add-transform";
        startInfo.EnvironmentVariables["NPHOTO_AUTOMATION_SCREENSHOT_PATH"] = screenshotPath;
        return startInfo;
    }

    private static async Task<Window> WaitForMainWindowAsync(Process process, UIA3Automation automation, TimeSpan timeout)
    {
        Window? window = null;
        await UiWait.WaitForConditionAsync(
            () =>
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException($"App process exited early with code {process.ExitCode}.");
                }

                window = automation.GetDesktop()
                    .FindFirstChild(child => child.ByProcessId(process.Id))
                    ?.AsWindow();
                return window is not null;
            },
            timeout,
            "Timed out waiting for app main window.");

        return window ?? throw new InvalidOperationException("Main window was not found.");
    }

    private static async Task<AutomationElement> WaitForStatusElementAsync(Window window, UIA3Automation automation, TimeSpan timeout)
    {
        AutomationElement? element = null;
        await UiWait.WaitForConditionAsync(
            () =>
            {
                element = window.FindFirstDescendant(automation.ConditionFactory.ByAutomationId("StatusText"));
                return element is not null;
            },
            timeout,
            "Timed out waiting for status text element.");

        return element ?? throw new InvalidOperationException("Status text element was not found.");
    }
}
