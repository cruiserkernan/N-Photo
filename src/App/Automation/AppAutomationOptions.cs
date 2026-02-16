namespace App.Automation;

internal sealed class AppAutomationOptions
{
    public const string AutomationModeEnvironmentVariable = "NPHOTO_AUTOMATION_MODE";
    public const string ScenarioEnvironmentVariable = "NPHOTO_AUTOMATION_SCENARIO";
    public const string ScreenshotPathEnvironmentVariable = "NPHOTO_AUTOMATION_SCREENSHOT_PATH";

    public static readonly AppAutomationOptions Disabled = new(false, null, null);

    private AppAutomationOptions(bool isEnabled, string? scenario, string? screenshotPath)
    {
        IsEnabled = isEnabled;
        Scenario = scenario;
        ScreenshotPath = screenshotPath;
    }

    public bool IsEnabled { get; }

    public string? Scenario { get; }

    public string? ScreenshotPath { get; }

    public static AppAutomationOptions FromEnvironment()
    {
        var isEnabled = IsEnabledValue(Environment.GetEnvironmentVariable(AutomationModeEnvironmentVariable));
        if (!isEnabled)
        {
            return Disabled;
        }

        var scenario = Normalize(Environment.GetEnvironmentVariable(ScenarioEnvironmentVariable));
        var screenshotPath = Normalize(Environment.GetEnvironmentVariable(ScreenshotPathEnvironmentVariable));
        return new AppAutomationOptions(isEnabled, scenario, screenshotPath);
    }

    private static bool IsEnabledValue(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        return rawValue.Trim() switch
        {
            "1" => true,
            "true" => true,
            "True" => true,
            _ => false
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
