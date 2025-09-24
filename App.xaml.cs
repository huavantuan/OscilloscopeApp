using System.Configuration;
using System.Reflection;
using System.Windows;
using System.IO;


namespace OscilloscopeApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromLibs;
        base.OnStartup(e);
    }

    private Assembly? ResolveFromLibs(object? sender, ResolveEventArgs args)
    {
        string dllName = new AssemblyName(args.Name).Name + ".dll";
        string libsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", dllName);

        if (File.Exists(libsPath))
            return Assembly.LoadFrom(libsPath);

        return null;
    }

}

