using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using App.Ui.Tests.Common;

namespace App.Ui.Headless.Tests;

public class MainWindowAutomationHeadlessTests
{
    [AvaloniaFact]
    public async Task StartupShellScenario_WritesScreenshot()
    {
        var screenshotPath = UiArtifactPaths.PrepareScreenshotPath("headless", "startup-shell");
        SetFontFallbackForHeadless();
        var window = new MainWindow();
        try
        {
            window.Show();
            await window.ExecuteAutomationScenarioForTestsAsync("startup-shell");
            SaveHeadlessFrame(window, screenshotPath);

            PngAssertions.AssertValid(screenshotPath);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task StartupAddTransformScenario_WritesScreenshot_AndSetsStatus()
    {
        var screenshotPath = UiArtifactPaths.PrepareScreenshotPath("headless", "startup-add-transform");
        SetFontFallbackForHeadless();
        var window = new MainWindow();
        try
        {
            window.Show();
            await window.ExecuteAutomationScenarioForTestsAsync("startup-add-transform");
            SaveHeadlessFrame(window, screenshotPath);

            PngAssertions.AssertValid(screenshotPath);

            Assert.Contains("Transform", window.GetStatusTextForAutomation(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            window.Close();
        }
    }

    private static void SetFontFallbackForHeadless()
    {
        if (Application.Current?.Resources is { } resources)
        {
            resources["FontFamily.Ui"] = FontFamily.Parse("Inter");
        }
    }

    private static void SaveHeadlessFrame(MainWindow window, string screenshotPath)
    {
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(screenshotPath);
    }
}
