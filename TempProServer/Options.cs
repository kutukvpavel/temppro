using CommandLine;

namespace TempProServer
{
    public class Options
    {
        public Options() { }

        [Option('p', "profile", Required = false, HelpText = "Temperature profile file path for automatic execution")]
        public string? ProfileFile { get; set; } = null;
    }
}