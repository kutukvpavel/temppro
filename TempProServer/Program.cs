using System;
using ConsoleTools;

namespace TempProServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Suckless TempPro software v0.1. Goodnight Moon!");

            var menu = new ConsoleMenu(args, level: 0)
                .Add("Connect to TempPRO", () => ConnectedMenuShow(args))
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

        public static void ConnectedMenuShow(string[] args)
        {
            var controller = new Controller(new Configuration());
            try
            {
                controller.Init();
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to initialize the controller interface, exiting.");
                Environment.Exit(1);
            }
            try
            {
                while (true)
                {
                    var menu = new ConsoleMenu(args, level: 0)
                    .Add("Read Temperature", () => {
                        Console.WriteLine($"Temp = {controller.GetTemperature()} deg C");
                        Console.WriteLine("Press any key to return...");
                        Console.ReadKey();
                    })
                    .Add("Set Temperature", () => {
                        Console.WriteLine("Enter temperature:");
                        string? s = Console.ReadLine();
                        if (double.TryParse(s, out double v))
                        {
                            controller.SetSetpoint(v);
                        }
                        else
                        {
                            Console.WriteLine("Wrong format. Press any key to return...");
                            Console.ReadKey();
                        }
                    })
                    .Add("Exit", () => Environment.Exit(0))
                    .Configure(config =>
                    {
                        config.Selector = "--> ";
                        config.EnableFilter = true;
                        config.Title = "Main menu";
                        config.EnableWriteTitle = true;
                        config.EnableBreadcrumb = true;
                    });

                    menu.Show();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to execute specified action. Exiting.");
                Environment.Exit(2);
            }
        }
    }
}
