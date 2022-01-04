using Client.Commands;
using Client.Options;
using CommandLine;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            //args = new string[] { "gather", "--station", "ibreto2", "--path", @"C:\app", "--date", "25/12/2021"};
            await Parser.Default.ParseArguments<GatherOptions>(args)
                .WithParsedAsync(opts => RunGather(opts));
        }

        private static async Task RunGather(GatherOptions opts) => await GatherCommand.Execute(opts);

    }
}
