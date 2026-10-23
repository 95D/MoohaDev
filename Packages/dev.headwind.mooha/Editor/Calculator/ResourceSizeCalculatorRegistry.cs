using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Headwind.Mooha.Editor.Calculator
{
    /// <summary>
    /// Collects every <see cref="IResourceSizeCalculator"/> in the project, keyed by resource type.
    /// <para>Discovery is by type rather than by a registration asset so that adding a calculator
    /// is the whole of adding a measurable type. The resulting key set doubles as the bake's
    /// population filter.</para>
    /// </summary>
    public static class ResourceSizeCalculatorRegistry
    {
        public static Dictionary<Type, IResourceSizeCalculator> Resolve()
        {
            var calculators = new Dictionary<Type, IResourceSizeCalculator>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<IResourceSizeCalculator>())
            {
                if (!IsDiscoverable(type))
                    continue;

                var calculator = (IResourceSizeCalculator)Activator.CreateInstance(type);
                if (calculators.TryGetValue(calculator.ResourceType, out var claimed))
                {
                    Debug.LogError(
                        $"[Mooha] '{type.FullName}' and '{claimed.GetType().FullName}' both "
                            + $"measure '{calculator.ResourceType.FullName}'. Keeping "
                            + $"'{claimed.GetType().FullName}'."
                    );
                    continue;
                }
                calculators.Add(calculator.ResourceType, calculator);
            }
            return calculators;
        }

        /// <summary>
        /// Skips the types a scan always turns up and cannot instantiate.
        /// <para>An abstract or open generic type is a base to build on, so finding one says
        /// nothing. A concrete type without a parameterless constructor is the opposite — it was
        /// written to be discovered and cannot be, which is reported.</para>
        /// </summary>
        private static bool IsDiscoverable(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
                return false;
            if (type.GetConstructor(Type.EmptyTypes) != null)
                return true;

            Debug.LogError(
                $"[Mooha] '{type.FullName}' implements IResourceSizeCalculator but has no "
                    + "parameterless constructor, so it cannot be used."
            );
            return false;
        }
    }
}
