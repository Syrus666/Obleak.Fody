using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Pdb;

namespace Obleak.Fody.Test
{
    public static class WeaveDebugHelper
    {
        public static Assembly WeavedAssembly { get; set; }

        public static void WeaveTestAssembly()
        {
            // Assembly has already been weaved as part of this run and doesn't need to be done again
            if (WeavedAssembly != null) return;

            var currentPath = AppDomain.CurrentDomain.BaseDirectory + @"\Obleak.Fody.Test.dll";

            var beforePath = Path.GetFullPath(currentPath);
            var beforePdb = beforePath.Replace(".dll", ".pdb");
            var weavedAssemblyPath = beforePath.Replace(".dll", "2.dll");
            var weavedPdbPath = beforePath.Replace(".dll", "2.pdb");

            File.Copy(beforePath, weavedAssemblyPath, true);
            File.Copy(beforePdb, weavedPdbPath, true);

            var resolver = new DefaultAssemblyResolver();

            foreach (var dir in resolver.GetSearchDirectories()) resolver.RemoveSearchDirectory(dir);

            resolver.AddSearchDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var errors = new List<string>();
            var warnings = new List<string>();

            using (var symbolStream = File.OpenRead(weavedPdbPath))
            {
                var readerParameters = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadSymbols = true,
                    SymbolStream = symbolStream,
                    SymbolReaderProvider = new PdbReaderProvider()
                };

                var moduleDefinition = ModuleDefinition.ReadModule(weavedAssemblyPath, readerParameters);

                var subscriptionWeavingTask = new SubscriptionObleakWeaver
                {
                    ModuleDefinition = moduleDefinition,
                    LogInfo = s => warnings.Add(s),
                    LogError = s => errors.Add(s),
                };

                subscriptionWeavingTask.Execute();

                if (errors.Any()) throw new Exception("Errors raised by the weaving process: " + string.Join(", ", errors));

                var reactiveCommandObleakTask = new ReactiveCommandObleakWeaver
                {
                    ModuleDefinition = moduleDefinition,
                    LogInfo = s => warnings.Add(s),
                    LogError = s => errors.Add(s),
                };

                reactiveCommandObleakTask.Execute();

                if (errors.Any()) throw new Exception("Errors raised by the weaving process: " + string.Join(", ", errors));

                moduleDefinition.Write(weavedAssemblyPath);
            }
            WeavedAssembly = Assembly.LoadFile(weavedAssemblyPath);
        }

        public static dynamic GetInstance(string className)
        {
            var type = WeavedAssembly.GetExportedTypes().First(x => x.Name == className);
            dynamic instance = WeavedAssembly.CreateInstance(type.FullName);
            return instance;
        }

    }
}
