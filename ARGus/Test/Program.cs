using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARGus;

namespace Test
{
    class Program
    {

        static void Main(string[] args)
        {
            bool success;
            var options = Argus.Parse<Options>(args, out success);

            if (!success)
                Argus.PrintUsage<Options>();
        }

        class Options
        {
            [ImplicitArgument(0, name: "Config file", description: "The path to the config file")]
            public string ConfigFile { get; set; }

            [ExplicitArgument("Date", 'd', "Sets the execution date")]
            public DateTime Date { get; set; }

            [SwitchArgument("Force", 'f', "Forces the operation")]
            public bool Force { get; set; }

            [ImplicitArgument(1, true, "Target files", "A list of all target files")]
            public IEnumerable<string> TargetFiles { get; set; }
        }
    }
}
