namespace WhisperTray.App;

public partial class App : System.Windows.Application
{
    private CompositionRoot? _root;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        _root = new CompositionRoot(Dispatcher);
        _root.Start();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _root?.Dispose();
        base.OnExit(e);
    }
}
