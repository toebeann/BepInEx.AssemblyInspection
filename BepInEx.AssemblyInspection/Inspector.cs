﻿using Mono.Cecil;
using Mono.Cecil.Rocks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BepInEx.AssemblyInspection
{
    public class Inspector
    {
        [JsonIgnore]
        public readonly IEnumerable<TypeDefinition> PluginTypes;
        [JsonIgnore]
        public readonly IEnumerable<TypeDefinition> PatcherTypes;
        
        public IEnumerable<string> Plugins => PluginTypes.Select(t => t.FullName);
        public IEnumerable<string> Patchers => PatcherTypes.Select(t => t.FullName);
        public readonly bool HasPlugins;
        public readonly bool HasPatchers;

        public Inspector(string filePath, IEnumerable<string> searchDirectories = null)
        {
            if (searchDirectories is null)
            {
                searchDirectories = Enumerable.Empty<string>();
            }

            searchDirectories = Directory.GetDirectories(RuntimeEnvironment.GetRuntimeDirectory(), "*", SearchOption.AllDirectories)
                .Concat(searchDirectories)
                .Concat(new[] { Path.GetDirectoryName(filePath) })
                .Distinct();

            using (var resolver = new DefaultAssemblyResolver())
            {
                foreach (var dir in searchDirectories)
                {
                    resolver.AddSearchDirectory(dir);
                }

                using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters { AssemblyResolver = resolver }))
                {
                    PluginTypes = GetPluginTypes(assemblyDefinition);
                    HasPlugins = PluginTypes.Any();
                    PatcherTypes = GetPatcherTypes(assemblyDefinition);
                    HasPatchers = PatcherTypes.Any();
                }
            }
        }

        public static IEnumerable<TypeDefinition> GetPluginTypes(AssemblyDefinition assemblyDefinition) =>
            assemblyDefinition.Modules
                .SelectMany(m => m.GetAllTypes())
                .Where(t => t.CustomAttributes.Select(a => a.AttributeType.FullName).Contains("BepInEx.BepInPlugin"));

        public static IEnumerable<TypeDefinition> GetPatcherTypes(AssemblyDefinition assemblyDefinition) =>
            assemblyDefinition.Modules
                .SelectMany(m => m.GetAllTypes())
                .Where(t => t.Properties.Any(p => p.Name == "TargetDLLs"
                        && p.PropertyType.FullName == "System.Collections.Generic.IEnumerable`1<System.String>")
                    && t.Methods.Any(m => m.Name == "Patch"
                        && m.ReturnType.FullName == typeof(void).FullName
                        && m.HasParameters
                        && m.Parameters.SingleOrDefault()?.ParameterType.FullName == typeof(AssemblyDefinition).FullName));
    }
}
