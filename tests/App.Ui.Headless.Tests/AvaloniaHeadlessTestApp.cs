using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

[assembly: AvaloniaTestApplication(typeof(App.Ui.Headless.Tests.AvaloniaHeadlessTestApp))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace App.Ui.Headless.Tests;

public static class AvaloniaHeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<global::App.App>()
            .UseSkia()
            .UseHeadless(
                new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = false
                })
            .WithInterFont()
            .LogToTrace();
    }
}
