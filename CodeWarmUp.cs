using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SplashAddonPlus
{
    /// <summary>
    /// Compiles the add-on's methods ahead of their first use (.NET compiles each method the first time it runs), so
    /// the first splash after Playnite starts does not wait for it. Runs on a background thread; safe to call any
    /// time, and a method that cannot be prepared is skipped.
    /// </summary>
    internal static class CodeWarmUp
    {
        /// <summary>Prepares every non-generic method and constructor with a body; returns how many were prepared.</summary>
        public static int CompileAll(Assembly assembly)
        {
            int prepared = 0;
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            catch
            {
                return 0;
            }

            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (Type type in types)
            {
                if (type == null || type.ContainsGenericParameters || type.IsInterface)
                {
                    continue;
                }

                MethodBase[] methods;
                try
                {
                    methods = Concat(type.GetMethods(all), type.GetConstructors(all));
                }
                catch
                {
                    continue;
                }

                foreach (MethodBase method in methods)
                {
                    if (method.IsAbstract || method.ContainsGenericParameters
                        || (method.GetMethodImplementationFlags() & (MethodImplAttributes.InternalCall | MethodImplAttributes.Runtime)) != 0
                        || (method.Attributes & MethodAttributes.PinvokeImpl) != 0)
                    {
                        continue;
                    }

                    try
                    {
                        RuntimeHelpers.PrepareMethod(method.MethodHandle);
                        prepared++;
                    }
                    catch
                    {
                        // Some methods cannot be prepared ahead; they are compiled when first used, as usual.
                    }
                }
            }

            return prepared;
        }

        private static MethodBase[] Concat(MethodInfo[] methods, ConstructorInfo[] constructors)
        {
            var result = new MethodBase[methods.Length + constructors.Length];
            Array.Copy(methods, result, methods.Length);
            Array.Copy(constructors, 0, result, methods.Length, constructors.Length);
            return result;
        }
    }
}
