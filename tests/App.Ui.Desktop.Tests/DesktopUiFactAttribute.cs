using App.Ui.Tests.Common;

namespace App.Ui.Desktop.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DesktopUiFactAttribute : FactAttribute
{
    public DesktopUiFactAttribute()
    {
        if (DesktopUiTestGate.TryGetSkipReason(out var reason))
        {
            Skip = reason;
        }
    }
}
