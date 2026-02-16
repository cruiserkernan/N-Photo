using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Editor.Application;
using Editor.Engine;
using Editor.IO;
using Editor.Nodes;

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
            var registry = new BuiltInNodeModuleRegistry();
            var session = new EditorSession(new BootstrapEditorEngine(registry), registry);
            desktop.MainWindow = new MainWindow(
                session,
                new SkiaImageLoader(),
                new SkiaImageExporter(),
                new JsonProjectDocumentStore());
        }

        base.OnFrameworkInitializationCompleted();
    }
}
