using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using App.Automation;
using Editor.Domain.Graph;

namespace App;

public partial class MainWindow
{
    private AppAutomationOptions _automationOptions = AppAutomationOptions.Disabled;
    private AppAutomationScenarioRunner? _automationRunner;
    private bool _automationScenarioStarted;
    private readonly CancellationTokenSource _automationCancellation = new();

    private bool IsAutomationModeEnabled => _automationOptions.IsEnabled;

    private void InitializeAutomation()
    {
        _automationOptions = AppAutomationOptions.FromEnvironment();
        _automationRunner = new AppAutomationScenarioRunner(this, _automationOptions);
    }

    private void StartAutomationScenarioIfEnabled()
    {
        if (_automationScenarioStarted || !IsAutomationModeEnabled || _automationRunner is null)
        {
            return;
        }

        _automationScenarioStarted = true;
        _ = _automationRunner.RunAsync(_automationCancellation.Token);
    }

    private void CancelAutomationScenario()
    {
        _automationCancellation.Cancel();
    }

    internal async Task WaitForAutomationRenderAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(
            () => { },
            DispatcherPriority.Background);
        await Dispatcher.UIThread.InvokeAsync(
            () => { },
            DispatcherPriority.Render);
    }

    internal async Task RunAutomationScenarioCoreAsync(string scenario, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (scenario.Trim())
        {
            case "startup-shell":
                break;
            case "startup-add-transform":
                await Dispatcher.UIThread.InvokeAsync(
                    () => AddNodeOfType(NodeTypes.Transform),
                    DispatcherPriority.Normal);
                break;
            default:
                throw new InvalidOperationException($"Unsupported automation scenario '{scenario}'.");
        }
    }

    internal void ApplyAutomationScenarioStatus(string scenario)
    {
        switch (scenario.Trim())
        {
            case "startup-shell":
                SetAutomationStatus("Automation scenario 'startup-shell' completed.");
                break;
            case "startup-add-transform":
                SetAutomationStatus("Node 'Transform' added.");
                break;
        }
    }

    internal void SetAutomationStatus(string message)
    {
        SetStatus(message);
    }

    internal string GetStatusTextForAutomation()
    {
        return StatusTextBlock.Text ?? string.Empty;
    }

    internal async Task ExecuteAutomationScenarioForTestsAsync(string scenario)
    {
        await RunAutomationScenarioCoreAsync(scenario, CancellationToken.None);
        await WaitForAutomationRenderAsync();
        ApplyAutomationScenarioStatus(scenario);
    }

    internal async Task CaptureAutomationScreenshotAsync(string screenshotPath)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath))
        {
            return;
        }

        var absolutePath = Path.GetFullPath(screenshotPath);
        var directory = Path.GetDirectoryName(absolutePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Automation screenshot path has no directory.");
        }

        Directory.CreateDirectory(directory);

        await Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var pixelWidth = Math.Max(1, (int)Math.Ceiling(Bounds.Width));
                var pixelHeight = Math.Max(1, (int)Math.Ceiling(Bounds.Height));
                using var bitmap = new RenderTargetBitmap(new PixelSize(pixelWidth, pixelHeight), new Vector(96, 96));
                bitmap.Render(this);
                bitmap.Save(absolutePath);
            },
            DispatcherPriority.Render);
    }
}
