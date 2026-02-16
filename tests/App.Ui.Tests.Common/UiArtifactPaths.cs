namespace App.Ui.Tests.Common;

public static class UiArtifactPaths
{
    public static string RepositoryRoot => FindRepositoryRoot();

    public static string PrepareScreenshotPath(string suite, string scenario)
    {
        var path = GetScreenshotPath(suite, scenario);
        var directory = Path.GetDirectoryName(path)
                        ?? throw new InvalidOperationException("Screenshot path has no directory.");

        Directory.CreateDirectory(directory);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return path;
    }

    public static string GetScreenshotPath(string suite, string scenario)
    {
        if (string.IsNullOrWhiteSpace(suite))
        {
            throw new ArgumentException("Suite name is required.", nameof(suite));
        }

        if (string.IsNullOrWhiteSpace(scenario))
        {
            throw new ArgumentException("Scenario name is required.", nameof(scenario));
        }

        return Path.Combine(
            RepositoryRoot,
            "artifacts",
            "ui-screenshots",
            suite,
            $"{scenario}.png");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "NPhoto.slnx");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
