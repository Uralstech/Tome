using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Tome;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    public App()
    {
        ServiceCollection services = new();
        services.AddSingleton<GamebananaService>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<ModInfo>();

        Services = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        (Services as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}

