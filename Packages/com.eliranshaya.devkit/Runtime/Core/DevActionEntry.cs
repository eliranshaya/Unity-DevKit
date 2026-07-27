#if DEVKIT_ENABLED
using System;
using System.Globalization;
using System.Text;

namespace DevKit.Internal
{
    /// <summary>What a row does when it is drawn.</summary>
    internal enum DevEntryKind
    {
        /// <summary>A button that invokes something.</summary>
        Action,

        /// <summary>A read-only label refreshed from a getter.</summary>
        Watch,

        /// <summary>A static piece of explanatory text. Used for the "no adapter" hint.</summary>
        Info,
    }

    /// <summary>Parameter types the panel knows how to render.</summary>
    internal enum DevParamKind
    {
        Int,
        Float,
        String,
        Bool,
        Enum,
    }

    /// <summary>One argument of an action, plus the value the user last typed for it.</summary>
    internal sealed class DevParam
    {
        internal string Name;
        internal DevParamKind Kind;
        internal Type Type;

        /// <summary>Live value, edited in place by the field widget and read back on invoke.</summary>
        internal object Value;

        /// <summary>Cached once for enum params so cycling the value allocates nothing.</summary>
        internal Array EnumValues;
        internal string[] EnumNames;

        internal string ValueAsString()
        {
            switch (Kind)
            {
                case DevParamKind.Int:
                    return ((int)Value).ToString(CultureInfo.InvariantCulture);
                case DevParamKind.Float:
                    return ((float)Value).ToString("0.###", CultureInfo.InvariantCulture);
                case DevParamKind.Bool:
                    return (bool)Value ? "ON" : "OFF";
                case DevParamKind.Enum:
                    return Value != null ? Value.ToString() : string.Empty;
                default:
                    return Value as string ?? string.Empty;
            }
        }

        /// <summary>
        /// Lenient parse: a half-typed field must never throw or clear itself while the user is
        /// still editing, so anything unparseable simply leaves the previous value alone.
        /// </summary>
        internal void ParseFromString(string raw)
        {
            switch (Kind)
            {
                case DevParamKind.Int:
                {
                    if (string.IsNullOrEmpty(raw) || raw == "-")
                    {
                        Value = 0;
                        return;
                    }
                    int parsed;
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        Value = parsed;
                    }
                    return;
                }
                case DevParamKind.Float:
                {
                    if (string.IsNullOrEmpty(raw) || raw == "-" || raw == "." || raw == "-.")
                    {
                        Value = 0f;
                        return;
                    }
                    float parsed;
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    {
                        Value = parsed;
                    }
                    return;
                }
                case DevParamKind.String:
                    Value = raw ?? string.Empty;
                    return;
            }
        }

        /// <summary>Steps a bool or enum to its next value. Wraps around.</summary>
        internal void CycleValue()
        {
            if (Kind == DevParamKind.Bool)
            {
                Value = !(bool)Value;
                return;
            }
            if (Kind != DevParamKind.Enum || EnumValues == null || EnumValues.Length == 0)
            {
                return;
            }

            int index = Array.IndexOf(EnumValues, Value);
            index = (index + 1) % EnumValues.Length;
            Value = EnumValues.GetValue(index);
        }
    }

    /// <summary>
    /// Internal record of one registered entry. Built once at registration time so that invoking
    /// it later allocates nothing: the argument array is reused and the invoker is a prebuilt
    /// delegate rather than a fresh reflection lookup.
    /// </summary>
    internal sealed class DevActionEntry
    {
        static readonly object[] NoArgs = new object[0];
        static readonly DevParam[] NoParams = new DevParam[0];

        internal DevEntryKind Kind;

        /// <summary>Full registration path, unique across the registry.</summary>
        internal string Path;

        /// <summary>Everything before the last separator, or "General".</summary>
        internal string Category;

        /// <summary>Last path segment - what the button reads.</summary>
        internal string Label;

        internal bool Confirm;
        internal int Order;

        internal DevParam[] Parameters = NoParams;

        /// <summary>Set for <see cref="DevEntryKind.Watch"/>.</summary>
        internal Func<string> Watch;

        /// <summary>Set for <see cref="DevEntryKind.Info"/>.</summary>
        internal string InfoText;

        Action<object[]> _invoker;
        object[] _args = NoArgs;

        internal static DevActionEntry CreateAction(string path, Action<object[]> invoker, DevParam[] parameters, bool confirm, int order)
        {
            DevActionEntry entry = new DevActionEntry();
            entry.Kind = DevEntryKind.Action;
            entry.Confirm = confirm;
            entry.Order = order;
            entry._invoker = invoker;
            entry.Parameters = parameters ?? NoParams;
            entry._args = entry.Parameters.Length == 0 ? NoArgs : new object[entry.Parameters.Length];
            entry.SplitPath(path);
            return entry;
        }

        internal static DevActionEntry CreateWatch(string path, Func<string> getter)
        {
            DevActionEntry entry = new DevActionEntry();
            entry.Kind = DevEntryKind.Watch;
            entry.Watch = getter;
            entry.SplitPath(path);
            return entry;
        }

        internal static DevActionEntry CreateInfo(string path, string text)
        {
            DevActionEntry entry = new DevActionEntry();
            entry.Kind = DevEntryKind.Info;
            entry.InfoText = text;
            entry.SplitPath(path);
            return entry;
        }

        /// <summary>
        /// Runs the action. Throws whatever the target threw - the panel catches it, shows a
        /// toast and logs the full stack. Never call this without a try/catch around it.
        /// </summary>
        internal void Invoke()
        {
            if (Kind != DevEntryKind.Action || _invoker == null)
            {
                return;
            }

            for (int i = 0; i < Parameters.Length; i++)
            {
                _args[i] = Parameters[i].Value;
            }
            _invoker(_args);
        }

        /// <summary>Current value of the watch getter, or the failure message if it threw.</summary>
        internal string ReadWatch(StringBuilder scratch)
        {
            if (Watch == null)
            {
                return string.Empty;
            }
            try
            {
                return Watch() ?? string.Empty;
            }
            catch (Exception e)
            {
                scratch.Length = 0;
                scratch.Append("<error: ").Append(e.GetType().Name).Append('>');
                return scratch.ToString();
            }
        }

        void SplitPath(string path)
        {
            Path = string.IsNullOrEmpty(path) ? "General/Unnamed" : path.Trim('/');

            int split = Path.LastIndexOf('/');
            if (split <= 0)
            {
                Category = "General";
                Label = Path;
            }
            else
            {
                Category = Path.Substring(0, split);
                Label = Path.Substring(split + 1);
            }

            if (string.IsNullOrEmpty(Label))
            {
                Label = Path;
            }
        }
    }
}
#endif
