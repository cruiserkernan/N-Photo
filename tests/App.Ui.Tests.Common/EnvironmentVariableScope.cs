namespace App.Ui.Tests.Common;

public sealed class EnvironmentVariableScope : IDisposable
{
    private readonly Dictionary<string, string?> _previousValues = new(StringComparer.Ordinal);

    public EnvironmentVariableScope(IReadOnlyDictionary<string, string?> variables)
    {
        foreach (var (key, value) in variables)
        {
            _previousValues[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public void Dispose()
    {
        foreach (var (key, previousValue) in _previousValues)
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }
}
