using CommandLine;
using System;

namespace Client.Options
{
    internal sealed class GatherOptions
    {
        public enum formats
        {
            json,
            csv
        }
        public enum units
        {
            imperials,
            metrics
        }

        [Option('s', "station", Required = true, HelpText = "Station identifier")]
        public string PwsIdentifier { get; set; }

        [Option('p', "path", Required = true, HelpText = "file destination")]
        public string Path { get; set; }

        [Option('f', "format", HelpText = "(optional) data's format")]
        public formats Format { get; set; } = formats.json;

        [Option('u', "units", HelpText = "(optional) data's units")]
        public units Units { get; set; } = units.imperials;

        [Option('d', "date", HelpText = "(optional) custom data's date")]
        public string Date { get; set; } = string.Empty;
    }
}
