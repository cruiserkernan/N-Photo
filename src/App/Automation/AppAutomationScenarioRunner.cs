namespace App.Automation;

internal sealed class AppAutomationScenarioRunner
{
    private readonly MainWindow _window;
    private readonly AppAutomationOptions _options;

    public AppAutomationScenarioRunner(MainWindow window, AppAutomationOptions options)
    {
        _window = window;
        _options = options;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled)
        {
            return;
        }

        try
        {
            await _window.WaitForAutomationRenderAsync();
            cancellationToken.ThrowIfCancellationRequested();

            var scenario = _options.Scenario;
            if (!string.IsNullOrWhiteSpace(scenario))
            {
                await _window.RunAutomationScenarioCoreAsync(scenario, cancellationToken);
                await _window.WaitForAutomationRenderAsync();
                _window.ApplyAutomationScenarioStatus(scenario);
            }
            else
            {
                _window.SetAutomationStatus("Automation mode enabled without scenario.");
            }

            if (!string.IsNullOrWhiteSpace(_options.ScreenshotPath))
            {
                await _window.CaptureAutomationScreenshotAsync(_options.ScreenshotPath);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation during app shutdown.
        }
        catch (Exception exception)
        {
            _window.SetAutomationStatus($"Automation failed: {exception.Message}");
        }
    }
}
