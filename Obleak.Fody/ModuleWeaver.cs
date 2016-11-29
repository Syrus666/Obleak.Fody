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
            var subscriptionObleakWeaver = new SubscriptionObleakWeaver
            {
                ModuleDefinition = ModuleDefinition,
                LogInfo = LogInfo,
                LogError = LogError
            };
            subscriptionObleakWeaver.Execute();

            var reactiveCommandObleakWeaver = new ReactiveCommandObleakWeaver
            {
                ModuleDefinition = ModuleDefinition,
                LogInfo = LogInfo,
                LogError = LogError
            };
            reactiveCommandObleakWeaver.Execute();
        }
    }
}
