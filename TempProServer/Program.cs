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

            Options? cliOptions = null;
            Parser.Default.ParseArguments<Options>(args).WithParsed((Options o) => cliOptions = o);
            if (cliOptions == null) return;

            var menu = new ConsoleMenu(args, level: 0)
                .Add("Connect to TempPRO", () => ConnectedMenuShow(args, cliOptions))
                .Add("Generate example cfg/profile", () => GenerateExamples(args, cliOptions))
                .Add("Exit", () => Environment.Exit(0))
                .Configure(config =>
                {
                    config.Selector = "--> ";
                    config.EnableFilter = true;
                    config.Title = "Main menu";
                    config.EnableWriteTitle = true;
                    config.EnableBreadcrumb = true;
                    config.ClearConsole = true;
                });

            menu.Show();
        }

        public static void GenerateExamples(string[] args, Options o)
        {
            Configuration.Save(Path.Combine(Environment.CurrentDirectory, "config.yaml"));
            Profile.Save(Path.Combine(Environment.CurrentDirectory, "example_profile.yaml"), Profile.Example);
            Console.WriteLine("Examples written to the working directoy. Press any key to return...");
            Console.ReadKey();
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
                            Console.Write($"\rTemp = {controller.GetTemperature():F2} deg C   ");
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
                    .Add("Set Ramp Rate", () => {
                        Console.WriteLine("Enter rate (deg C / min):");
                        string? s = Console.ReadLine();
                        if (double.TryParse(s, out double v))
                        {
                            controller.SetRampControl(true);
                            controller.SetRampRate(v);
                            Console.WriteLine("OK");
                        }
                        else
                        {
                            controller.SetRampControl(false);
                            Console.WriteLine("Wrong format. Ramp control disabled. Press any key to return...");
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
                            Console.WriteLine($"P.S. Expecting config at: {configPath}");
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
            string serviceLogPath = Path.Combine(Environment.CurrentDirectory, $"temppro_service_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
            StreamWriter serviceLogger = File.AppendText(serviceLogPath);
            serviceLogger.AutoFlush = true;
            var prf = Profile.Load(path);
            var exec = new Execution(prf, ctrl, Configuration.Instance);
            exec.ExceptionOccurred += (o, e) => Console.WriteLine($"Exception during profile execution: {e}");
            try
            {
                var status = ctrl.GetConnectionStatus();
                if (!status.Item1) {
                    Log($"Controller connection error: {status.Item2}", serviceLogger);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to check controller connection status: {ex}", serviceLogger);
                return;
            }
            try
            {
                var err = exec.VerifyAndCalculate(serviceLogger);
                serviceLogger.Flush();
                if (err != null)
                {
                    Log($"Profile did not pass validation: {err}", serviceLogger);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log($"Error during profile validation: {ex}", serviceLogger);
            }
            float progress = 0;
            try
            {
                ((Progress<float>)exec.Progress).ProgressChanged += (object? s, float p) => progress = p;
                exec.Start();
            }
            catch (Exception ex)
            {
                Log($"Failed to start execution: {ex}", serviceLogger);
                exec.Abort();
                return;
            }
            TextWriter? logWriterHandle = null;
            if (prf.EnableLog)
            {
                var logWriter = File.AppendText(logPath);
                logWriterHandle = logWriter;
                logWriter.AutoFlush = true;
                exec.LogEvent += (o, t) => {
                    logWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}, {1:F2}", DateTime.Now, t));
                };
            }
            while (!CancellationSource.IsCancellationRequested && exec.State == ExecutionStates.Running)
            {
                if (ctrl.ErrorOccurred)
                {
                    Log("Controller communication error, aborting", serviceLogger);
                    break;
                }
                Console.Write($"\rT = {exec.CurrentTemperature:F1}, set = {exec.CurrentSetpoint:F1}, step = {exec.SegmentIndex} ({progress:F0}%), tr = {exec.TimeRemaining}");
                Thread.Sleep(1000);
            }
            if (CancellationSource.IsCancellationRequested || ctrl.ErrorOccurred) exec.Abort();
            if (logWriterHandle != null) logWriterHandle.Close();
        }

        private static void Log(string msg, TextWriter w)
        {
            Console.WriteLine(msg);
            w.WriteLine(msg);
        }
    }
}
