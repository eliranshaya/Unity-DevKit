#if DEVKIT_ENABLED
using System;
using System.Globalization;

namespace DevKit.Internal
{
    /// <summary>What a table cell draws.</summary>
    internal enum DevCellKind
    {
        /// <summary>A row name. Optionally a button, optionally highlighted.</summary>
        Label,

        /// <summary>A read-only value, polled.</summary>
        Text,

        /// <summary>An editable int or float.</summary>
        Number,

        /// <summary>An on/off button.</summary>
        Toggle,

        /// <summary>A dropdown over fixed options.</summary>
        Choice,

        /// <summary>The same button on every row.</summary>
        Button,
    }

    /// <summary>
    /// One column of a <see cref="DevTable"/>, with the row type erased: every accessor takes a row
    /// index. <see cref="DevTable{TRow}"/> builds these, which is what lets the panel draw any table
    /// without knowing what its rows are. Only the fields that belong to the column's kind are set.
    /// </summary>
    internal sealed class DevTableColumn
    {
        internal DevCellKind Kind;
        internal string Header;

        /// <summary>Label, Text: what the cell reads.</summary>
        internal Func<int, string> ReadText;

        /// <summary>Number. A double carries every int exactly, so one path serves both.</summary>
        internal Func<int, double> ReadNumber;
        internal Action<int, double> WriteNumber;
        internal bool Integer;
        internal string Format;

        /// <summary>Toggle.</summary>
        internal Func<int, bool> ReadBool;
        internal Action<int, bool> WriteBool;

        /// <summary>Choice.</summary>
        internal string[] Options;
        internal Func<int, int> ReadIndex;
        internal Action<int, int> WriteIndex;

        /// <summary>Button: what every row's button reads.</summary>
        internal string Caption;

        /// <summary>Button, and Label when it is a button: what a tap does.</summary>
        internal Action<int> Click;

        /// <summary>Optional. On a Label: highlighted. On a Button: enabled.</summary>
        internal Func<int, bool> Flag;

        /// <summary>Whether the cell takes taps, and therefore needs a full touch target.</summary>
        internal bool IsInteractive
        {
            get { return Kind == DevCellKind.Label ? Click != null : Kind != DevCellKind.Text; }
        }

        /// <summary>The column in messages: its header, or the button caption when the header is blank.</summary>
        internal string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(Header))
                {
                    return Header;
                }
                return Kind == DevCellKind.Button && !string.IsNullOrEmpty(Caption) ? Caption : Kind.ToString();
            }
        }

        internal string FormatNumber(double value)
        {
            if (Integer)
            {
                return Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
            }
            return ((float)value).ToString(Format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reads a typed number. Accepts a comma as the decimal separator too: a phone keyboard in a
        /// German or French locale offers nothing else.
        /// </summary>
        internal bool TryParseNumber(string raw, out double value)
        {
            value = 0d;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            string normalized = raw.Trim().Replace(',', '.');
            double parsed;
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ||
                double.IsNaN(parsed) || double.IsInfinity(parsed))
            {
                return false;
            }

            value = Integer ? Math.Round(parsed) : parsed;
            return true;
        }
    }

    /// <summary>A button under a table that acts on the whole of it.</summary>
    internal sealed class DevTableAction
    {
        internal string Label;
        internal Action Invoke;
        internal bool Confirm;
    }
}
#endif
