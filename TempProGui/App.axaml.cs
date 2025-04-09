using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TempProGui.ViewModels;
using TempProGui.Views;
using TempProServer;
using CommandLine;
using System;
using System.IO;
using LLibrary;
using TempProGui.Models;

namespace TempProGui;

public partial class App : Application
{
    public L Logger { get; } = new L();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(GenerateModel(desktop.Args)),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public Model GenerateModel(string[]? args)
    {
        //Load config
        string? configPath = null;
        Parser.Default.ParseArguments<Options>(args).WithParsed((o) => {
            if (o.ConfigPath != null) configPath = o.ConfigPath;
        });
        configPath ??= Path.Combine(Environment.CurrentDirectory, "temppro.yaml");
        if (File.Exists(configPath))
        {
            Logger.Info($"Using config file path '{configPath}'");
            try
            {
                Configuration.Load(configPath);
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Can't load configuration: {ex}");
                Environment.Exit(1);
            }
        }
        else
        {
            Logger.Info($"Using default configuration, creating config file at '{configPath}'");
            try
            {
                Configuration.Save(configPath);   
            }
            catch (Exception ex)
            {
                Logger.Error($"Can't save configuration: {ex}");
            }
        }
        //Create model
        try
        {
            Model m = new(Configuration.Instance);
            return m;
        }
        catch (Exception ex)
        {
            Logger.Fatal($"Initialization failed: {ex}");
            Environment.Exit(2);
        }
        throw new InvalidOperationException();
    }
}