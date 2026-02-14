using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Editor.Engine;
using Editor.IO;

namespace App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(
                new BootstrapEditorEngine(),
                new StubImageLoader(),
                new StubImageExporter());
        }

        base.OnFrameworkInitializationCompleted();
    }
}
