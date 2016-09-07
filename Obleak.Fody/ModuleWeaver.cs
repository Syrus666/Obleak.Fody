using System;
using Mono.Cecil;

namespace Obleak.Fody
{
    public class ModuleWeaver
    {
        public ModuleDefinition ModuleDefinition { get; set; }

        // Will log an MessageImportance.High message to MSBuild. 
        public Action<string> LogInfo { get; set; }

        // Will log an error message to MSBuild. OPTIONAL
        public Action<string> LogError { get; set; }

        public void Execute()
        {
            var obleakWeaver = new ObleakWeaver
            {
                ModuleDefinition = ModuleDefinition,
                LogInfo = LogInfo,
                LogError = LogError
            };
            obleakWeaver.Execute();
        }
    }
}
