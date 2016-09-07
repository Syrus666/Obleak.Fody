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
    /// Extension methods to reduce some of the bioler plate when working with Mono.Cecil.
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
        internal static MethodDefinition GetDisposeMehod(this TypeDefinition type)
        {
            return type.GetMethods().FirstOrDefault(x => x.Name == "Dispose" && !x.HasParameters) ??
                   type.BaseType.Resolve().GetDisposeMehod();
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
