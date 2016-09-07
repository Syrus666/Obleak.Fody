using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Obleak.Fody
{
    public class ObleakWeaver
    {
        private const string COMPOSITE_DISPOSABLE_FIELD_NAME = "$ObleakCompositeDisposable";

        private readonly Func<Instruction, TypeReference, string, bool> _isExpectedMethodCall =
            (instruction, expectedReturnType, expectedName) =>
                instruction?.Operand != null &&
                (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference &&
                ((MethodReference)instruction.Operand).ReturnType.FullName == expectedReturnType.FullName &&
                ((MethodReference)instruction.Operand).FullName.Contains(expectedName);

        public ModuleDefinition ModuleDefinition { get; set; }

        // Will log an MessageImportance.High message to MSBuild. OPTIONAL
        public Action<string> LogInfo { get; set; }

        // Will log an error message to MSBuild. OPTIONAL
        public Action<string> LogError { get; set; }

        public void Execute()
        {
            LogInfo("Obleak weaving");

            // Validate we have the dependencies we need
            var rxCore = ModuleDefinition.AssemblyReferences.Where(x => x.Name == "System.Reactive.Core").OrderByDescending(x => x.Version).FirstOrDefault();
            if (rxCore == null)
            {
                LogError("Could not find assembly: System.Reactive.Core (" + string.Join(", ", ModuleDefinition.AssemblyReferences.Select(x => x.Name)) + ")");
                return;
            }

            var mscorlib = ModuleDefinition.AssemblyReferences.Where(x => x.Name == "mscorlib").OrderByDescending(x => x.Version).FirstOrDefault();
            if (mscorlib == null)
            {
                LogError("Could not find assembly: System (" + string.Join(", ", ModuleDefinition.AssemblyReferences.Select(x => x.Name)) + ")");
                return;
            }

            // Get the IDisposable type
            var disposableType = new TypeReference("System", "IDisposable", ModuleDefinition, mscorlib);

            // Get the CompositeDisposable type and required methods
            var compositeDisposableType = new TypeReference("System.Reactive.Disposables", "CompositeDisposable", ModuleDefinition, rxCore);
            var compositeDisposableTypeResolved = compositeDisposableType.Resolve();
            if (compositeDisposableTypeResolved == null) throw new Exception("compositeDisposableTypeResolved is null");
            var compositeDisposableCtor = ModuleDefinition.Import(compositeDisposableTypeResolved.Methods.Single(m => m.IsConstructor && !m.HasParameters));
            var compositeDisposableDisposeMethod = ModuleDefinition.Import(compositeDisposableTypeResolved.Methods.Single(m => m.Name == "Dispose"));

            // Get the obleak core and attribute
            var obleakCore = ModuleDefinition.AssemblyReferences.Where(x => x.Name == "Obleak.Fody.Core").OrderByDescending(x => x.Version).FirstOrDefault();
            if (obleakCore == null)
            {
                LogInfo("Could not find assembly: Obleak.Fody.Core (" + string.Join(", ", ModuleDefinition.AssemblyReferences.Select(x => x.Name)) + ")");
                return;
            }

            var obleakAttribute = ModuleDefinition.FindType("Obleak.Fody.Core", "ObleakAttribute", obleakCore);
            if (obleakAttribute == null) throw new Exception("obleakAttribute is null");

            var disposableExtensions = new TypeReference("Obleak.Fody.Core", "Extensions", ModuleDefinition, obleakCore).Resolve();
            if (disposableExtensions == null) throw new Exception("disposableExtensions is null");

            var handleWithExtensionMethod = ModuleDefinition.Import(disposableExtensions.Methods.Single(x => x.Name == "HandleWith"));
            if (handleWithExtensionMethod == null) throw new Exception("handleWithExtensionMethod is null");

            // Any class where the Obleak attribute appears on the class, a constructor or a method
            var targets =
                ModuleDefinition.GetAllTypes()
                    .Where(
                        x =>
                            x.IsDefined(obleakAttribute) || 
                            x.GetConstructors().Any(y => y.IsDefined(obleakAttribute)) ||
                            x.GetMethods().Any(y => y.IsDefined(obleakAttribute)));

            targets.ForEach(target =>
            {
                LogInfo($"Weaving target: {target.FullName}");
                // We can only weave classes which are disposable 
                if (!target.IsDisposable())
                {
                    LogError($"Target class {target.FullName} is not disposable and therefore cannot be weaved with Obleak");
                    return;
                }

                // If this is class wide process every method, else only tackle what has been attributed. If the class + specific methods / constructors
                // have the attribute, still process everything

                // Note: we only need one composite disposable per target type
                var isClassWide = target.IsDefined(obleakAttribute);

                // The methods we're going to weave
                var methods = target.Methods.Where(x => isClassWide || x.IsDefined(obleakAttribute));

                // Declare a field for the composite disposable
                var compositeDisposableField = new FieldDefinition(COMPOSITE_DISPOSABLE_FIELD_NAME + target.Name, FieldAttributes.Private, compositeDisposableType);
                target.Fields.Add(compositeDisposableField);

                // Initialise this in all of the constructors in this class (not it's inheritance hierarchy) -- hence target.Methods usage
                target.Methods.Where(m => m.IsConstructor).ForEach(constructor =>
                {
                    constructor.Body.Emit(il =>
                    {
                        var first = constructor.Body.Instructions[0]; // first instruction

                        // Instructions equivalent of: this.$ObleakCompositeDisposable = new CompositeDisposable();
                        // But as it's done as the first instruction set when decompiled this is actually
                        // private CompositeDisposable $ObleakCompositeDisposable = new CompositeDisposable();
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // this
                        il.InsertBefore(first, il.Create(OpCodes.Newobj, compositeDisposableCtor)); // new CompositeDisposable from ctor
                        il.InsertBefore(first, il.Create(OpCodes.Stfld, compositeDisposableField.BindDefinition(target))); // store new obj in fld
                    });
                });

                // For every .Subscribe() which returns a disposable call the .Add method on the compositeDisposableField
                methods.Where(m => m.HasBody && m.Body.Instructions.Any(i => _isExpectedMethodCall(i, disposableType, "Subscribe"))).ForEach(method =>
                {
                    method.Body.Emit(il =>
                    {
                        var subscribes = method.Body.Instructions.Where(i => _isExpectedMethodCall(i, disposableType, "Subscribe")).ToArray();
                        subscribes.ForEach(i =>
                        {
                            var next = i.Next;
                            il.InsertBefore(next, il.Create(OpCodes.Ldarg_0)); // this
                            il.InsertBefore(next, il.Create(OpCodes.Ldfld, compositeDisposableField.BindDefinition(target)));
                            il.InsertBefore(next, il.Create(OpCodes.Call, handleWithExtensionMethod)); // Call .HandleWith($ObleakCompositeDisposable)
                        });
                    });
                });

                // Does the target itself have a dispose method or is it inheriting the implementation? 
                // If doesn't have one of it's own we need to add one and call the base.Dispose() as we need to clean up this
                // new local composite disposable.
                if (!target.GetMethods().Any(m => m.Name == "Dispose" && !m.HasParameters))
                {
                    // Not one locally get the one from one of the base classes and use it as a base definite
                    var baseDispose = target.GetDisposeMehod();

                    // Create a new empty dispose method to add to the target
                    var disposeMethod = new MethodDefinition(baseDispose.Name, baseDispose.Attributes, baseDispose.ReturnType)
                    {
                        IsVirtual = true,
                        IsReuseSlot = true,
                        IsHideBySig = true,
                        Body = new MethodBody(baseDispose)
                    };
                    disposeMethod.Body.Emit(il =>
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, baseDispose);
                        il.Emit(OpCodes.Ret);
                    });

                    baseDispose.IsVirtual = true;
                    baseDispose.IsReuseSlot = true;
                    baseDispose.IsHideBySig = true;
                    target.Methods.Add(disposeMethod);
                }

                // Find the dispose method and append the instructions at the end to clean up the composite disposable
                var dispose = target.GetDisposeMehod();

                dispose.Body.Emit(il =>
                {
                    var last = dispose.Body.Instructions.Last(i => i.OpCode == OpCodes.Ret && (i.Next == null || dispose.Body.Instructions.Last() == i));
                    il.InsertBefore(last, il.Create(OpCodes.Ldarg_0)); // this
                    il.InsertBefore(last, il.Create(OpCodes.Ldfld, compositeDisposableField.BindDefinition(target)));
                    il.InsertBefore(last, il.Create(OpCodes.Callvirt, compositeDisposableDisposeMethod)); // call $ObleakCompositeDisposable.Dispose();
                });

                LogInfo($"Completed weaving target: {target.FullName}");
            });
        }
    }
}