using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Tome.Services;

namespace Tome;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    public App()
    {
        ServiceCollection services = new();
        services.AddSingleton<GamebananaService>();
        services.AddSingleton<MainWindow>();

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

