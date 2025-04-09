using CommandLine;

namespace TempProGui
{
    public class Options
    {
        public Options() { }

        [Option('c', "config", HelpText = "Custom configuration file path", Required = false)]
        public string? ConfigPath { get; set; } = null;
    }
}