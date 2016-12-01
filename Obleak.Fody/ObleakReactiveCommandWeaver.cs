using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Obleak.Fody
{
    public class ObleakReactiveCommandWeaver
    {
        public ModuleDefinition ModuleDefinition { get; set; }

        // Will log an MessageImportance.High message to MSBuild. OPTIONAL
        public Action<string> LogInfo { get; set; }

        // Will log an error message to MSBuild. OPTIONAL
        public Action<string> LogError { get; set; }

        public void Execute()
        {
            LogInfo("ReactiveCommandObleak weaving");

            // Validate we have the dependencies we need
            var mscorlib = ModuleDefinition.FindAssembly("mscorlib", LogError);
            if (mscorlib == null) return;

            var obleakCore = ModuleDefinition.FindAssembly("Obleak.Fody.Core", LogError);
            if (obleakCore == null) return;

            var reactiveUiCore = ModuleDefinition.FindAssembly("ReactiveUI", LogError);
            if (reactiveUiCore == null) return;

            // Get the IDisposable and IReactiveCommand types
            var disposableType = new TypeReference("System", "IDisposable", ModuleDefinition, mscorlib);
            var disposeableTypeResolved = ModuleDefinition.Import(disposableType).Resolve();
            var disposeMethod = ModuleDefinition.Import(disposeableTypeResolved.Methods.First(x => x.Name == "Dispose"));

            var reactiveCommandType = new TypeReference("ReactiveUI", "IReactiveCommand", ModuleDefinition, reactiveUiCore);

            var obleakCommandAttribute = ModuleDefinition.FindType("Obleak.Fody.Core", "ObleakReactiveCommandAttribute", obleakCore);
            if (obleakCommandAttribute == null) throw new Exception("obleakCommandAttribute is null");

            // Any class where the ObleakCommand attribute appears on a proeprty
            var targets =
                ModuleDefinition.GetAllTypes().Where(x => x.IsDefined(obleakCommandAttribute) || x.Properties.Any(y => y.IsDefined(obleakCommandAttribute)));

            targets.ForEach(target =>
            {
                LogInfo($"Weaving target: {target.FullName}");
                // We can only weave classes which are disposable 
                if (!target.IsDisposable())
                {
                    LogError($"Target class {target.FullName} is not disposable and therefore cannot be weaved with Obleak Command");
                    return;
                }

                // Is this for all reactive commands in the class?
                var isClassWide = target.IsDefined(obleakCommandAttribute);

                // The command properties we're going to call dispose on
                var properties = target.Properties.Where(p => (isClassWide || p.IsDefined(obleakCommandAttribute)) && 
                                                              // That implements IReactiveCommand
                                                              reactiveCommandType.IsAssignableFrom(p.PropertyType) &&
                                                              // And is disposable
                                                              disposableType.IsAssignableFrom(p.PropertyType));

                // If his type doesn't already have a dispose method create one
                var hasDisposeMethod = target.HasDisposeMethod();
                if (!hasDisposeMethod && target.HasGenericParameters)
                {
                    LogError($"Automatically generating a Dispose method for {target.Name} is not supported due to generics in it's inheritance hierarchy. " +
                             $"You need to create an empty parameterless Dispose() and re-build to use the Obleak weavers");
                    return;
                }
                if (!hasDisposeMethod) target.CreateDisposeMethod();

                // Find the dispose method and append the instructions at the end to clean up the composite disposable
                var dispose = target.GetDisposeMethod();

                dispose.Body.Emit(il =>
                {
                    var last = dispose.Body.Instructions.Last(i => i.OpCode == OpCodes.Ret && (i.Next == null || dispose.Body.Instructions.Last() == i));

                    properties.ForEach(p =>
                    {
                        var propertyGetMethod = p.GetMethod;

                        // Now load this property, call the get method and then call dispose
                        il.InsertBefore(last, il.Create(OpCodes.Ldarg_0)); // this
                        il.InsertBefore(last, il.Create(OpCodes.Call, propertyGetMethod));
                        il.InsertBefore(last, il.Create(OpCodes.Callvirt, disposeMethod));
                    });
                });

                LogInfo($"Completed weaving target: {target.FullName}");
            });
        }
    }
}