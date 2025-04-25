using System;
using System.Globalization;
using System.IO;
using System.Threading;
using CommandLine;
using ConsoleTools;

namespace TempProServer
{
    public static class Program
    {
        public static readonly CancellationTokenSource CancellationSource = new();

        public static void Console_Cancel(object? sender, ConsoleCancelEventArgs e)
        {
            CancellationSource.Cancel();
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Suckless TempPro software v0.1. Goodnight Moon!");
            Console.CancelKeyPress += Console_Cancel;

            var menu = new ConsoleMenu(args, level: 0)
                .Add("Connect to TempPRO", () => Parser.Default.ParseArguments<Options>(args).WithParsed((Options o) => ConnectedMenuShow(args, o)))
                .Add("Generate example cfg/profile", () => Parser.Default.ParseArguments<Options>(args).WithParsed((Options o) => GenerateExamples(args, o)))
                .Add("Exit", () => Environment.Exit(0))
                .Configure(config =>
                {
                    config.Selector = "--> ";
                    config.EnableFilter = true;
                    config.Title = "Main menu";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = true;
                    config.ClearConsole = false;
                });

            menu.Show();
        }

        public static void GenerateExamples(string[] args, Options o)
        {
            
        }

        public static void ConnectedMenuShow(string[] args, Options o)
        {
            var controller = new Controller(new Configuration());
            try
            {
                controller.Init();
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to initialize the controller interface, exiting.");
                controller.CloseConnection();
                Environment.Exit(1);
            }
            try
            {
                while (true)
                {
                    var menu = new ConsoleMenu(args, level: 0)
                    .Add("Read Temperature", () => {
                        Console.WriteLine("Press any key to return...");
                        while (!Console.KeyAvailable && !CancellationSource.IsCancellationRequested)
                        {
                            Console.Write($"\rTemp = {controller.GetTemperature()} deg C");
                            Thread.Sleep(1000);
                        }
                        Console.ReadKey(true);
                        Console.WriteLine();
                    })
                    .Add("Set Temperature", () => {
                        Console.WriteLine("Enter temperature:");
                        string? s = Console.ReadLine();
                        if (double.TryParse(s, out double v))
                        {
                            controller.SetSetpoint(v);
                            Console.WriteLine("OK");
                        }
                        else
                        {
                            Console.WriteLine("Wrong format. Press any key to return...");
                            Console.ReadKey();
                        }
                    })
                    .Add("Execute Profile", () => {
                        string configPath = Path.Combine(Environment.CurrentDirectory, "config.yaml");
                        if (File.Exists(configPath))
                        {
                            Configuration.Load(configPath);
                        }
                        else
                        {
                            Configuration.Save(configPath);
                            Console.WriteLine("WARNING: no config file was present, written one with defaults");
                        }
                        if (o.ProfileFile != null)
                        {
                            ExecuteProfile(o.ProfileFile, controller);
                        }
                        else
                        {
                            Console.WriteLine("No profile path was specified on the command line");
                        }
                    })
                    .Add("Exit", () => { 
                        try
                        {
                            controller.CloseConnection();
                        }
                        catch (Exception)
                        {
                            
                        }
                        Environment.Exit(0);
                    })
                    .Configure(config =>
                    {
                        config.Selector = "--> ";
                        config.EnableFilter = true;
                        config.Title = "Main menu";
                        config.EnableWriteTitle = true;
                        config.EnableBreadcrumb = true;
                        config.ClearConsole = false;
                    });

                    menu.Show();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to execute specified action. Exiting.");
                controller.CloseConnection();
                Environment.Exit(2);
            }
        }

        public static void ExecuteProfile(string path, Controller ctrl)
        {
            if (!Path.IsPathFullyQualified(path))
            {
                path = Path.GetFullPath(path, Environment.CurrentDirectory);
            }
            string logPath = Path.Combine(Environment.CurrentDirectory, $"temppro_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
            var prf = Profile.Load(path);
            var exec = new Execution(prf, ctrl, Configuration.Instance);
            try
            {
                var status = ctrl.GetConnectionStatus();
                if (!status.Item1) {
                    Console.WriteLine($"Controller connection error: {status.Item2}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check controller connection status: {ex}");
                return;
            }
            try
            {
                var err = exec.VerifyAndCalculate();
                if (err != null)
                {
                    Console.WriteLine($"Profile did not pass validation: {err}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during profile validation: {ex}");
            }
            try
            {
                exec.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start execution: {ex}");
                exec.Abort();
                return;
            }
            using var logWriter = File.AppendText(logPath);
            while (!CancellationSource.IsCancellationRequested && exec.State == ExecutionStates.Running)
            {
                if (ctrl.ErrorOccurred)
                {
                    Console.WriteLine("Controller communication error, aborting");
                    break;
                }
                Console.Write($"\rT = {exec.CurrentTemperature:F1}, set = {exec.CurrentSetpoint:F1}, step = {exec.SegmentIndex} ({exec.Progress:F0}%), tr = {exec.TimeRemaining}");
                if (prf.EnableLog)
                {
                    logWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}, {1:F2}", DateTime.Now, exec.CurrentTemperature));
                }
                Thread.Sleep(1000);
            }
            if (CancellationSource.IsCancellationRequested || ctrl.ErrorOccurred) exec.Abort();
        }
    }
}
