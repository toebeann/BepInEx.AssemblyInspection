using Fclp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BepInEx.AssemblyInspection
{
    internal class Program
    {
        public class Arguments
        {
            [Flags]
            public enum BepInExType
            {
                Plugins = 1,
                Patchers = 2,
                All = Plugins | Patchers
            }

            public string FilePath { get; set; }
            public BepInExType Types { get; set; }
            public List<string> SearchDirectories { get; set; }
        }

        static void Main(string[] args)
        {
            var p = new FluentCommandLineParser<Arguments>();

            p.Setup(arg => arg.FilePath)
                .As('f', "file-path")
                .Required();

            p.Setup(arg => arg.Types)
                .As('t', "types")
                .SetDefault(Arguments.BepInExType.All);

            p.Setup(arg => arg.SearchDirectories)
                .As('s', "search-directories")
                .SetDefault(new List<string>());

            var parsed = p.Parse(args);

            if (parsed.HasErrors)
            {
                Console.WriteLine(parsed.ErrorText);
                Environment.Exit(1);
                return;
            }

            var inspector = new Inspector(p.Object.FilePath, p.Object.SearchDirectories);
            switch (p.Object.Types)
            {
                case Arguments.BepInExType.All:
                    Console.WriteLine(JsonConvert.SerializeObject(inspector));
                    break;
                case Arguments.BepInExType.Plugins:
                    Console.WriteLine(JsonConvert.SerializeObject(inspector.HasPlugins));
                    break;
                case Arguments.BepInExType.Patchers:
                    Console.WriteLine(JsonConvert.SerializeObject(inspector.HasPatchers));
                    break;
            }
        }
    }
}
