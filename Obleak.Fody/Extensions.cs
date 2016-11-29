using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Obleak.Fody
{
    internal static class Extensions
    {
        internal static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (action == null) throw new ArgumentNullException(nameof(action));
            foreach (T item in sequence) action(item);
        }
    }

    /// <summary>
    /// Extension methods to reduce some of the boiler plate when working with Mono.Cecil.
    /// Several of these extension methods have been sourced, with love, from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
    /// </summary>
    internal static class CecilExtensions
    {
        // Taken from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
        internal static void Emit(this MethodBody body, Action<ILProcessor> il)
        {
            il(body.GetILProcessor());
        }

        // Taken from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
        internal static bool IsDefined(this IMemberDefinition member, TypeReference attributeType)
        {
            return member.HasCustomAttributes && member.CustomAttributes.Any(x => x.AttributeType.FullName == attributeType.FullName);
        }

        // Taken from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
        internal static TypeReference FindType(this ModuleDefinition currentModule, string @namespace, string typeName, IMetadataScope scope = null, params string[] typeParameters)
        {
            var result = new TypeReference(@namespace, typeName, currentModule, scope);
            foreach (var typeParameter in typeParameters)
            {
                result.GenericParameters.Add(new GenericParameter(typeParameter, result));
            }
            return result;
        }

        // Taken from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
        internal static FieldReference BindDefinition(this FieldReference field, TypeReference genericTypeDefinition)
        {
            if (!genericTypeDefinition.HasGenericParameters)
                return field;

            var genericDeclaration = new GenericInstanceType(genericTypeDefinition);
            foreach (var parameter in genericTypeDefinition.GenericParameters)
            {
                genericDeclaration.GenericArguments.Add(parameter);
            }
            var reference = new FieldReference(field.Name, field.FieldType, genericDeclaration);
            return reference;
        }

        // Taken from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
        public static bool IsAssignableFrom(this TypeReference baseType, TypeReference type, Action<string> logger = null)
        {
            return baseType.Resolve().IsAssignableFrom(type.Resolve(), logger);
        }

        // Taken from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
        public static bool IsAssignableFrom(this TypeDefinition baseType, TypeDefinition type, Action<string> logger = null)
        {
            logger = logger ?? (x => { });

            Queue<TypeDefinition> queue = new Queue<TypeDefinition>();
            queue.Enqueue(type);

            while (queue.Any())
            {
                var current = queue.Dequeue();
                logger(current.FullName);

                if (baseType.FullName == current.FullName)
                    return true;

                if (current.BaseType != null)
                    queue.Enqueue(current.BaseType.Resolve());

                foreach (var @interface in current.Interfaces)
                {
                    queue.Enqueue(@interface.Resolve());
                }
            }

            return false;
        }

        // Taken from https://github.com/kswoll/ReactiveUI.Fody/blob/master/ReactiveUI.Fody/CecilExtensions.cs
        public static GenericInstanceMethod MakeGenericMethod(this MethodReference method, params TypeReference[] genericArguments)
        {
            var result = new GenericInstanceMethod(method);
            foreach (var argument in genericArguments)
                result.GenericArguments.Add(argument);
            return result;
        }

        /// <summary>
        /// Looks for the latest version of the name assembly within the AssemblyReferences of the ModuleDefinition.
        /// If the assembly is not found any provided action is invoked
        /// </summary>
        /// <param name="moduleDefinition">The module definitiion to search</param>
        /// <param name="assemblyFullName">Full name of the assembly</param>
        /// <param name="onNotFound">Action to invoke if the assembly is not found</param>
        /// <returns>The found assembly reference or null</returns>
        internal static AssemblyNameReference FindAssembly(this ModuleDefinition moduleDefinition, string assemblyFullName, Action<string> onNotFound = null)
        {
            var assembly = moduleDefinition.AssemblyReferences.Where(x => x.Name == assemblyFullName).OrderByDescending(x => x.Version).FirstOrDefault();
            if (assembly == null)
                onNotFound?.Invoke($"Could not find assembly: {assemblyFullName} (" + string.Join(", ", moduleDefinition.AssemblyReferences.Select(x => x.Name)) + ")");
            return assembly;
        }

        /// <summary>
        /// Calculate if the type, or any of it's base types implement the IDiposable interface
        /// </summary>
        internal static bool IsDisposable(this TypeDefinition type)
        {
            return type.Interfaces.Any(i => i.FullName == "System.IDisposable") || 
                   (type.BaseType != null && type.BaseType.Resolve().IsDisposable());
        }

        /// <summary>
        /// Recurisively find the IDiposable.Dispose() implementation on a type
        /// </summary>
        internal static MethodDefinition GetDisposeMethod(this TypeDefinition type)
        {
            if(!type.IsDisposable()) throw new Exception($"Type: {type.FullName} is not disposable");

            return type.GetMethods().FirstOrDefault(x => x.Name == "Dispose" && !x.HasParameters) ??
                   type.BaseType.Resolve().GetDisposeMethod();
        }

        /// <summary>
        /// Finds the Dispose method which needs to be overriden on the base type of an inherticance hierarchy
        /// </summary>
        internal static MethodDefinition GetBaseDispose(this TypeDefinition type)
        {
            var typeRef = type.BaseType;
            while (typeRef.Resolve().HasDisposeMethod())
                typeRef = typeRef.Resolve().BaseType;

            return typeRef.Resolve().GetDisposeMethod();
        }

        /// <summary>
        /// Calculates if the @param type has a default dispose methods (so, no parameters)
        /// </summary>
        internal static bool HasDisposeMethod(this TypeDefinition type)
        {
            return type.GetMethods().Any(m => m.Name == "Dispose" && !m.HasParameters);
        }

        /// <summary>
        /// Generates a dispose method for the type and adds it.
        /// If the base dispose is not virtual, make it so.
        /// </summary>
        /// <param name="type"></param>
        internal static void CreateDisposeMethod(this TypeDefinition type)
        {
            // Not one locally get the one from one of the base classes and use it as a base definite
            var baseDispose = type.GetDisposeMethod();
            baseDispose.IsVirtual = true;
            baseDispose.IsReuseSlot = true;
            baseDispose.IsHideBySig = true;

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

            type.Methods.Add(disposeMethod);
        }

        /// <summary>
        /// Recursively get all methods and constructors from a type and it's inheritance hierarchy
        /// </summary>
        internal static IEnumerable<MethodDefinition> GetAllMethodsAndConstructors(this TypeDefinition type)
        {
            if (!type.GetMethods().Any() && !type.GetConstructors().Any() && type.BaseType == null) return Enumerable.Empty<MethodDefinition>();
            return type.BaseType?.Resolve().GetAllMethodsAndConstructors().Concat(type.GetMethods().Concat(type.GetConstructors())) ?? Enumerable.Empty<MethodDefinition>();
        }
    }
}
