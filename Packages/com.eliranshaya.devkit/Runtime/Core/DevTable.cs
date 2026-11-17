using System;
using System.Collections.Generic;

#if DEVKIT_ENABLED
using DevKit.Internal;
#endif

namespace DevKit
{
    /// <summary>
    /// A grid of live controls for <see cref="DevActions.RegisterTable"/>. Build one with
    /// <see cref="DevTable{TRow}"/> - this base type only exists so the registry can hold tables of
    /// any row type.
    /// </summary>
    /// <remarks>
    /// Always compiled, so game code that builds a table still compiles with <c>DEVKIT_ENABLED</c>
    /// undefined. With the symbol off the builder methods record nothing, and a table built inline
    /// in the <see cref="DevActions.RegisterTable"/> call is erased along with the call.
    /// </remarks>
    public abstract class DevTable
    {
#if DEVKIT_ENABLED
        internal readonly List<DevTableColumn> Columns = new List<DevTableColumn>(8);
        internal readonly List<DevTableAction> Actions = new List<DevTableAction>(2);

        internal abstract int RowCount { get; }

        /// <summary>
        /// The first label column: what a row is called in search results and error messages.
        /// Null when the table has none.
        /// </summary>
        internal DevTableColumn NameColumn
        {
            get
            {
                for (int i = 0; i < Columns.Count; i++)
                {
                    if (Columns[i].Kind == DevCellKind.Label)
                    {
                        return Columns[i];
                    }
                }
                return null;
            }
        }
#endif

        // Only DevTable<TRow> derives from this.
        internal DevTable()
        {
        }
    }

    /// <summary>
    /// Builds a table whose rows are <typeparamref name="TRow"/> items - an enum's values, your own
    /// objects, plain indices. Add columns in the order they should appear; each binds a getter and,
    /// where the cell is editable, a setter.
    /// <para>
    /// There is no Run button anywhere. A typed value is written the moment the edit is committed -
    /// Enter, Done on a phone keyboard, or tapping anywhere else - and a dropdown or a toggle writes
    /// on the tap. After every change the whole table is read again, so a setter that moves other
    /// columns too (picking a preset that fills in three values, say) shows its effect at once.
    /// Cells are also polled at the watch rate while the table is on screen, so changes made
    /// elsewhere appear without reopening the panel.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// DevActions.RegisterTable("Audio/Mixer", new DevTable&lt;AudioSource&gt;(sources)
    ///     .Label("Source", s =&gt; s.name, onClick: s =&gt; s.Play())
    ///     .Float("Volume", s =&gt; s.volume, (s, v) =&gt; s.volume = v)
    ///     .Toggle("Mute", s =&gt; s.mute, (s, v) =&gt; s.mute = v)
    ///     .Action("Stop All", StopAll));
    /// </code>
    /// </example>
    /// <typeparam name="TRow">What one row stands for. Anything works; the table never inspects it.</typeparam>
    public sealed class DevTable<TRow> : DevTable
    {
#if DEVKIT_ENABLED
        readonly TRow[] _rows;
#endif

        /// <param name="rows">The rows, in display order. Copied - register the table again to change them.</param>
        public DevTable(IEnumerable<TRow> rows)
        {
#if DEVKIT_ENABLED
            _rows = rows != null ? new List<TRow>(rows).ToArray() : new TRow[0];
#endif
        }

#if DEVKIT_ENABLED
        internal override int RowCount { get { return _rows.Length; } }
#endif

        /// <summary>
        /// Row names, left aligned in a column sized to the longest one. Read when the table is drawn
        /// rather than polled, so they should not change while it is on screen. The first label
        /// column also names the rows for search: typing part of a name narrows the table to it.
        /// </summary>
        /// <param name="header">Column title.</param>
        /// <param name="text">The row's name.</param>
        /// <param name="onClick">Optional. Turns each name into a button - give it the row's main action.</param>
        /// <param name="highlight">
        /// Optional. True draws the name in the accent colour with a bar beside it - for example to
        /// show which rows differ from their defaults. Polled with the other cells.
        /// </param>
        public DevTable<TRow> Label(string header, Func<TRow, string> text,
            Action<TRow> onClick = null, Func<TRow, bool> highlight = null)
        {
#if DEVKIT_ENABLED
            if (!Accept(header, text, text))
            {
                return this;
            }

            TRow[] rows = _rows;
            DevTableColumn column = AddColumn(DevCellKind.Label, header);
            column.ReadText = delegate (int row) { return text(rows[row]); };

            if (onClick != null)
            {
                column.Click = delegate (int row) { onClick(rows[row]); };
            }
            if (highlight != null)
            {
                column.Flag = delegate (int row) { return highlight(rows[row]); };
            }
#endif
            return this;
        }

        /// <summary>A read-only value, polled at the watch rate like <see cref="DevActions.RegisterWatch"/>.</summary>
        public DevTable<TRow> Text(string header, Func<TRow, string> get)
        {
#if DEVKIT_ENABLED
            if (!Accept(header, get, get))
            {
                return this;
            }

            TRow[] rows = _rows;
            DevTableColumn column = AddColumn(DevCellKind.Text, header);
            column.ReadText = delegate (int row) { return get(rows[row]); };
#endif
            return this;
        }

        /// <summary>An editable float, written when the edit is committed.</summary>
        /// <param name="format">How the value is shown, as for <see cref="float.ToString(string)"/>.</param>
        public DevTable<TRow> Float(string header, Func<TRow, float> get, Action<TRow, float> set,
            string format = "0.###")
        {
#if DEVKIT_ENABLED
            if (!Accept(header, get, set))
            {
                return this;
            }

            TRow[] rows = _rows;
            DevTableColumn column = AddColumn(DevCellKind.Number, header);
            column.Format = string.IsNullOrEmpty(format) ? "0.###" : format;
            column.ReadNumber = delegate (int row) { return get(rows[row]); };
            column.WriteNumber = delegate (int row, double value) { set(rows[row], (float)value); };
#endif
            return this;
        }

        /// <summary>An editable int, written when the edit is committed.</summary>
        public DevTable<TRow> Int(string header, Func<TRow, int> get, Action<TRow, int> set)
        {
#if DEVKIT_ENABLED
            if (!Accept(header, get, set))
            {
                return this;
            }

            TRow[] rows = _rows;
            DevTableColumn column = AddColumn(DevCellKind.Number, header);
            column.Integer = true;
            column.ReadNumber = delegate (int row) { return get(rows[row]); };
            column.WriteNumber = delegate (int row, double value)
            {
                // A typed 1e12 would otherwise wrap around to garbage on the cast.
                double clamped = Math.Max(int.MinValue, Math.Min(int.MaxValue, Math.Round(value)));
                set(rows[row], (int)clamped);
            };
#endif
            return this;
        }

        /// <summary>An on/off cell. A tap writes the opposite of what it shows.</summary>
        public DevTable<TRow> Toggle(string header, Func<TRow, bool> get, Action<TRow, bool> set)
        {
#if DEVKIT_ENABLED
            if (!Accept(header, get, set))
            {
                return this;
            }

            TRow[] rows = _rows;
            DevTableColumn column = AddColumn(DevCellKind.Toggle, header);
            column.ReadBool = delegate (int row) { return get(rows[row]); };
            column.WriteBool = delegate (int row, bool value) { set(rows[row], value); };
#endif
            return this;
        }

        /// <summary>
        /// A dropdown. Tapping the cell lists <paramref name="options"/> beside it; picking one
        /// writes its index. Sized to the longest option.
        /// </summary>
        /// <param name="get">Index of the current option. Anything out of range shows "-".</param>
        public DevTable<TRow> Choice(string header, string[] options, Func<TRow, int> get, Action<TRow, int> set)
        {
#if DEVKIT_ENABLED
            if (!Accept(header, get, set))
            {
                return this;
            }
            if (options == null || options.Length == 0)
            {
                DevKitLog.Warning(string.Format("Skipped table column '{0}': it has no options.", header));
                return this;
            }

            TRow[] rows = _rows;
            DevTableColumn column = AddColumn(DevCellKind.Choice, header);
            column.Options = (string[])options.Clone();
            column.ReadIndex = delegate (int row) { return get(rows[row]); };
            column.WriteIndex = delegate (int row, int index) { set(rows[row], index); };
#endif
            return this;
        }

        /// <summary>A dropdown over an enum's values, named as declared.</summary>
        public DevTable<TRow> Choice<TEnum>(string header, Func<TRow, TEnum> get, Action<TRow, TEnum> set)
            where TEnum : struct, Enum
        {
#if DEVKIT_ENABLED
            if (!Accept(header, get, set))
            {
                return this;
            }

            TEnum[] values = (TEnum[])Enum.GetValues(typeof(TEnum));
            if (values.Length == 0)
            {
                DevKitLog.Warning(string.Format("Skipped table column '{0}': {1} has no values.", header, typeof(TEnum).Name));
                return this;
            }

            string[] names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                names[i] = values[i].ToString();
            }

            EqualityComparer<TEnum> comparer = EqualityComparer<TEnum>.Default;
            return Choice(header, names,
                delegate (TRow item)
                {
                    TEnum current = get(item);
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (comparer.Equals(values[i], current))
                        {
                            return i;
                        }
                    }
                    return -1;
                },
                delegate (TRow item, int index) { set(item, values[index]); });
#else
            return this;
#endif
        }

        /// <summary>The same button on every row - "Reset", say.</summary>
        /// <param name="header">Column title. May be empty: the buttons label themselves.</param>
        /// <param name="label">What every row's button reads.</param>
        /// <param name="enabled">Optional. False dims that row's button and ignores taps. Polled.</param>
        public DevTable<TRow> Button(string header, string label, Action<TRow> onClick, Func<TRow, bool> enabled = null)
        {
#if DEVKIT_ENABLED
            if (!Accept(header, onClick, onClick))
            {
                return this;
            }

            TRow[] rows = _rows;
            DevTableColumn column = AddColumn(DevCellKind.Button, header);
            column.Caption = label ?? string.Empty;
            column.Click = delegate (int row) { onClick(rows[row]); };

            if (enabled != null)
            {
                column.Flag = delegate (int row) { return enabled(rows[row]); };
            }
#endif
            return this;
        }

        /// <summary>
        /// A full-width button under the table, for something that acts on all of it - "Reset All".
        /// It behaves like an action row: a toast reports the outcome, and <paramref name="confirm"/>
        /// asks first.
        /// </summary>
        public DevTable<TRow> Action(string label, Action action, bool confirm = false)
        {
#if DEVKIT_ENABLED
            if (string.IsNullOrEmpty(label) || action == null)
            {
                DevKitLog.Warning(string.Format("Skipped table action '{0}': it needs a label and a callback.", label));
                return this;
            }

            DevTableAction entry = new DevTableAction();
            entry.Label = label;
            entry.Invoke = action;
            entry.Confirm = confirm;
            Actions.Add(entry);
#endif
            return this;
        }

#if DEVKIT_ENABLED
        DevTableColumn AddColumn(DevCellKind kind, string header)
        {
            DevTableColumn column = new DevTableColumn();
            column.Kind = kind;
            column.Header = header ?? string.Empty;
            Columns.Add(column);
            return column;
        }

        static bool Accept(string header, object read, object write)
        {
            if (read != null && write != null)
            {
                return true;
            }

            DevKitLog.Warning(string.Format("Skipped table column '{0}': a callback is null.", header));
            return false;
        }
#endif
    }
}
