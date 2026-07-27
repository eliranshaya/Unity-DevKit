#if DEVKIT_ENABLED
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevKit.Internal
{
    /// <summary>
    /// Storage for every registered entry plus the one-shot reflection scan that discovers
    /// <see cref="DevActionAttribute"/> methods.
    /// <para>
    /// The scan is lazy on purpose: it runs the first time the panel is opened, never in
    /// <c>Awake</c> or <c>Start</c>. A player who never presses the hotkey pays nothing for it.
    /// </para>
    /// </summary>
    internal static class DevActionRegistry
    {
        /// <summary>
        /// Assemblies that cannot contain user actions. Skipping them is what keeps the scan in
        /// the low milliseconds instead of the hundreds.
        /// </summary>
        static readonly string[] SkippedPrefixes =
        {
            "System",
            "Unity",
            "mscorlib",
            "netstandard",
            "Mono.",
            "nunit",
            "JetBrains",
        };

        const BindingFlags MethodFlags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.DeclaredOnly;

        static readonly List<DevActionEntry> Entries = new List<DevActionEntry>(64);
        static readonly Dictionary<string, DevActionEntry> ByPath = new Dictionary<string, DevActionEntry>(64, StringComparer.Ordinal);

        static bool _scanned;
        static int _version;

        /// <summary>Bumped on every change so the panel knows its cached layout is stale.</summary>
        internal static int Version { get { return _version; } }

        internal static List<DevActionEntry> All { get { return Entries; } }

        /// <summary>
        /// Static state survives entering play mode when domain reload is disabled, which would
        /// otherwise duplicate every entry on the second run. Wiping here covers both setups.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnEnterPlayMode()
        {
            Clear();
            DevKitAdapter.Reset();
        }

        internal static void Clear()
        {
            Entries.Clear();
            ByPath.Clear();
            BuiltinModules.Reset();
            _scanned = false;
            _version++;
        }

        // ---------------------------------------------------------------- registration

        internal static void AddAction(string path, Action action, bool confirm)
        {
            if (!Validate(path, action)) return;

            Add(DevActionEntry.CreateAction(path, delegate { action(); }, null, confirm, 0));
        }

        internal static void AddAction<T>(string path, Action<T> action, bool confirm)
        {
            if (!Validate(path, action)) return;

            DevParam parameter = TryCreateParam(typeof(T), "value", null);
            if (parameter == null)
            {
                DevKitLog.Warning(string.Format(
                    "'{0}' was not registered: parameter type '{1}' is not supported. " +
                    "Use int, float, string, bool or an enum.", path, typeof(T).Name));
                return;
            }

            Action<object[]> invoker = delegate (object[] args) { action((T)args[0]); };
            Add(DevActionEntry.CreateAction(path, invoker, new[] { parameter }, confirm, 0));
        }

        internal static void AddWatch(string path, Func<string> getter)
        {
            if (!Validate(path, getter)) return;

            Add(DevActionEntry.CreateWatch(path, getter));
        }

        internal static void AddInfo(string path, string text)
        {
            if (string.IsNullOrEmpty(path)) return;

            Add(DevActionEntry.CreateInfo(path, text));
        }

        internal static void Remove(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string key = path.Trim('/');
            DevActionEntry existing;
            if (!ByPath.TryGetValue(key, out existing))
            {
                return;
            }

            ByPath.Remove(key);
            Entries.Remove(existing);
            _version++;
        }

        static bool Validate(string path, object callback)
        {
            if (string.IsNullOrEmpty(path))
            {
                DevKitLog.Warning("Ignored a registration with an empty path.");
                return false;
            }
            if (callback == null)
            {
                DevKitLog.Warning(string.Format("Ignored '{0}': the callback is null.", path));
                return false;
            }
            return true;
        }

        static void Add(DevActionEntry entry)
        {
            DevActionEntry existing;
            if (ByPath.TryGetValue(entry.Path, out existing))
            {
                // Re-registering the same path replaces rather than duplicates, so hot reloading
                // a script or re-running an installer stays idempotent.
                Entries.Remove(existing);
            }

            ByPath[entry.Path] = entry;
            Entries.Add(entry);
            _version++;
        }

        // ---------------------------------------------------------------- scan

        /// <summary>
        /// Discovers every <see cref="DevActionAttribute"/> in the loaded user assemblies and
        /// installs the built-in modules. Runs once; subsequent calls are a single bool check.
        /// </summary>
        internal static void ScanAssemblies()
        {
            if (_scanned)
            {
                return;
            }
            _scanned = true;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                string name;
                try
                {
                    name = assembly.GetName().Name;
                }
                catch
                {
                    continue;
                }

                if (ShouldSkip(name))
                {
                    continue;
                }

                // One bad assembly - a ReflectionTypeLoadException from a half-loaded plugin, say -
                // must not take the whole panel down with it.
                try
                {
                    ScanAssembly(assembly);
                }
                catch (Exception e)
                {
                    DevKitLog.Warning(string.Format("Skipped assembly '{0}' during the scan: {1}", name, e.Message));
                }
            }

            BuiltinModules.Install();

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 50)
            {
                DevKitLog.Info(string.Format(
                    "Assembly scan took {0} ms and found {1} entries.",
                    stopwatch.ElapsedMilliseconds, Entries.Count));
            }
        }

        static bool ShouldSkip(string assemblyName)
        {
            for (int i = 0; i < SkippedPrefixes.Length; i++)
            {
                if (assemblyName.StartsWith(SkippedPrefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        static void ScanAssembly(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Partial results are still worth having.
                types = e.Types;
            }

            if (types == null)
            {
                return;
            }

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(MethodFlags);
                }
                catch
                {
                    continue;
                }

                for (int m = 0; m < methods.Length; m++)
                {
                    MethodInfo method = methods[m];
                    DevActionAttribute attribute;
                    try
                    {
                        attribute = (DevActionAttribute)Attribute.GetCustomAttribute(method, typeof(DevActionAttribute), false);
                    }
                    catch
                    {
                        continue;
                    }

                    if (attribute != null)
                    {
                        TryRegisterMethod(type, method, attribute);
                    }
                }
            }
        }

        static void TryRegisterMethod(Type declaringType, MethodInfo method, DevActionAttribute attribute)
        {
            string where = declaringType.FullName + "." + method.Name;

            if (method.IsAbstract || method.ContainsGenericParameters)
            {
                DevKitLog.Warning(string.Format("Skipped '{0}': abstract and generic methods cannot be invoked.", where));
                return;
            }

            if (!method.IsStatic && !typeof(Component).IsAssignableFrom(declaringType))
            {
                DevKitLog.Warning(string.Format(
                    "Skipped '{0}': instance actions must live on a Component so DevKit can find a target.", where));
                return;
            }

            ParameterInfo[] parameterInfos = method.GetParameters();
            DevParam[] parameters = parameterInfos.Length == 0 ? null : new DevParam[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                ParameterInfo info = parameterInfos[i];
                if (info.IsOut || info.ParameterType.IsByRef)
                {
                    DevKitLog.Warning(string.Format(
                        "Skipped '{0}': parameter '{1}' is by reference, which the panel cannot render.", where, info.Name));
                    return;
                }

                object defaultValue = info.HasDefaultValue ? info.DefaultValue : null;
                DevParam parameter = TryCreateParam(info.ParameterType, info.Name, defaultValue);
                if (parameter == null)
                {
                    DevKitLog.Warning(string.Format(
                        "Skipped '{0}': parameter '{1}' is of unsupported type '{2}'. " +
                        "Use int, float, string, bool or an enum.", where, info.Name, info.ParameterType.Name));
                    return;
                }
                parameters[i] = parameter;
            }

            Action<object[]> invoker;
            if (method.IsStatic)
            {
                invoker = delegate (object[] args) { method.Invoke(null, args); };
            }
            else
            {
                Type targetType = declaringType;
                invoker = delegate (object[] args)
                {
                    Object target = DevKitCompat.FindFirst(targetType);
                    if (target == null)
                    {
                        throw new InvalidOperationException(
                            "No live " + targetType.Name + " in the loaded scenes to invoke this on.");
                    }
                    method.Invoke(target, args);
                };
            }

            Add(DevActionEntry.CreateAction(attribute.Path, invoker, parameters, attribute.Confirm, attribute.Order));
        }

        // ---------------------------------------------------------------- parameters

        static DevParam TryCreateParam(Type type, string name, object defaultValue)
        {
            DevParam parameter = new DevParam();
            parameter.Name = string.IsNullOrEmpty(name) ? "value" : name;
            parameter.Type = type;

            if (type == typeof(int))
            {
                parameter.Kind = DevParamKind.Int;
                parameter.Value = defaultValue is int ? defaultValue : 0;
            }
            else if (type == typeof(float))
            {
                parameter.Kind = DevParamKind.Float;
                parameter.Value = defaultValue is float ? defaultValue : 0f;
            }
            else if (type == typeof(string))
            {
                parameter.Kind = DevParamKind.String;
                parameter.Value = defaultValue as string ?? string.Empty;
            }
            else if (type == typeof(bool))
            {
                parameter.Kind = DevParamKind.Bool;
                parameter.Value = defaultValue is bool && (bool)defaultValue;
            }
            else if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                if (values.Length == 0)
                {
                    return null;
                }

                parameter.Kind = DevParamKind.Enum;
                parameter.EnumValues = values;
                parameter.EnumNames = Enum.GetNames(type);
                parameter.Value = defaultValue != null && defaultValue.GetType() == type
                    ? defaultValue
                    : values.GetValue(0);
            }
            else
            {
                return null;
            }

            return parameter;
        }
    }
}
#endif
