#if DEVKIT_ENABLED
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>What a table needs from the panel hosting it.</summary>
    internal interface IDevTableHost
    {
        /// <summary>
        /// Runs a table callback. A throw becomes a red toast plus the full stack in the console,
        /// and false comes back; the panel stays open either way.
        /// </summary>
        bool TryRun(string path, Action action);

        /// <summary>Runs a table-wide action the way an action row runs: confirmed if asked, toasted either way.</summary>
        void RunAction(string path, string label, Action action, bool confirm);

        /// <summary>Opens the option list against <paramref name="anchor"/>.</summary>
        void ShowChoice(RectTransform anchor, float minWidth, string[] options, int selected, Action<int> onPick);
    }

    /// <summary>
    /// The width envelope of one column. The header and every cell of the column share it, which is
    /// what keeps the columns aligned from row to row.
    /// </summary>
    internal struct DevTableSize
    {
        internal float Min;
        internal float Preferred;
        internal float Flexible;

        internal void Apply(GameObject cell, float height)
        {
            DevPanelBuilder.SetLayout(cell, Min, Preferred, height, height, Flexible, 0f);
        }
    }

    /// <summary>
    /// Draws a <see cref="DevTable"/>: a header, one row per item, and the table's actions under it.
    /// <para>
    /// Every column is measured against what it holds - the longest name, the widest option, a
    /// sample number - and sized to that, so no column is wider than its content needs. Spare room
    /// goes to the editable columns, up to <see cref="DevPanelTheme.TableMaxStretch"/>; on a pane
    /// that is too narrow, columns squeeze down to <see cref="DevPanelTheme.TableMinShare"/> of
    /// their width before the row overflows.
    /// </para>
    /// <para>
    /// After any edit, tap or action, every cell re-reads its getter: a setter is free to move
    /// other columns, and the table shows it at once. The panel also polls the table at the watch
    /// rate while it is on screen.
    /// </para>
    /// </summary>
    internal sealed class DevTableView
    {
        readonly DevActionEntry _entry;
        readonly IDevTableHost _host;
        readonly List<DevTableCell> _cells = new List<DevTableCell>(64);

        /// <summary>Set once the pane is rebuilt and this view's GameObjects are on their way out.</summary>
        bool _released;

        DevTableView(DevActionEntry entry, IDevTableHost host)
        {
            _entry = entry;
            _host = host;
        }

        // ---------------------------------------------------------------- search

        /// <summary>Whether any row's name contains <paramref name="filter"/>, ignoring case.</summary>
        internal static bool AnyRowMatches(DevTable table, string filter)
        {
            DevTableColumn names = table.NameColumn;
            if (names == null)
            {
                return false;
            }

            for (int row = 0; row < table.RowCount; row++)
            {
                if (Matches(ReadName(names, row), filter))
                {
                    return true;
                }
            }
            return false;
        }

        static bool Matches(string name, string filter)
        {
            return name != null && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ---------------------------------------------------------------- construction

        /// <param name="caption">Shown above the header when not null - the full path, while searching.</param>
        /// <param name="rowFilter">When not null, only rows whose name contains it are drawn.</param>
        internal static DevTableView Build(Transform parent, DevActionEntry entry, string caption, string rowFilter,
            IDevTableHost host)
        {
            DevTable table = entry.Table;
            List<DevTableColumn> columns = table.Columns;
            DevTableView view = new DevTableView(entry, host);

            string[] names = ReadNames(entry);
            List<int> rows = new List<int>(table.RowCount);
            for (int row = 0; row < table.RowCount; row++)
            {
                if (rowFilter == null || Matches(names[row], rowFilter))
                {
                    rows.Add(row);
                }
            }

            DevTableSize[] sizes = new DevTableSize[columns.Count];
            float gaps = DevPanelTheme.TableCellGap * (columns.Count - 1);
            float natural = gaps;
            float squeezed = gaps;
            for (int c = 0; c < columns.Count; c++)
            {
                sizes[c] = Measure(columns[c], rows, names);
                natural += sizes[c].Preferred;
                squeezed += sizes[c].Min;
            }

            // The block spans the pane; the grid inside it takes the width its columns want, plus a
            // capped share of whatever is left. Anything beyond the cap stays empty pane.
            RectTransform block = DevPanelBuilder.NewRect(entry.Label, parent);
            HorizontalLayoutGroup blockLayout = DevPanelBuilder.AddHorizontal(block.gameObject, 0f, new RectOffset(0, 0, 0, 0));
            blockLayout.childAlignment = TextAnchor.UpperLeft;

            RectTransform grid = DevPanelBuilder.NewRect("Grid", block);
            DevPanelBuilder.AddVertical(grid.gameObject, DevPanelTheme.TableRowGap, new RectOffset(0, 0, 0, 0));
            DevPanelBuilder.SetLayout(grid.gameObject, squeezed, natural * DevPanelTheme.TableMaxStretch, -1f, -1f, 0f, -1f);

            if (caption != null)
            {
                Text text = DevPanelBuilder.NewText("Caption", grid, caption, DevPanelTheme.FontSizeSmall, DevPanelTheme.TextDim);
                DevPanelBuilder.SetLayout(text.gameObject, -1f, -1f,
                    DevPanelTheme.TableHeaderHeight, DevPanelTheme.TableHeaderHeight, -1f, -1f);
            }

            BuildHeader(grid, columns, sizes);

            for (int i = 0; i < rows.Count; i++)
            {
                view.BuildRow(grid, rows[i], names[rows[i]], sizes);
            }

            if (table.RowCount == 0)
            {
                Text empty = DevPanelBuilder.NewText("Empty", grid, "No rows.", DevPanelTheme.FontSizeBody, DevPanelTheme.TextDim);
                DevPanelBuilder.SetLayout(empty.gameObject, -1f, -1f, DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, -1f, -1f);
            }

            if (table.Actions.Count > 0)
            {
                RectTransform spacer = DevPanelBuilder.NewRect("Spacer", grid);
                DevPanelBuilder.SetLayout(spacer.gameObject, -1f, -1f,
                    DevPanelTheme.TableActionsGap, DevPanelTheme.TableActionsGap, -1f, -1f);

                for (int i = 0; i < table.Actions.Count; i++)
                {
                    view.BuildAction(grid, table.Actions[i]);
                }
            }

            view.Refresh();
            return view;
        }

        /// <summary>
        /// Natural width of a column: the widest thing it can hold plus its padding, never narrower
        /// than its header, never narrower than a touch target when it takes taps.
        /// </summary>
        static DevTableSize Measure(DevTableColumn column, List<int> rows, string[] names)
        {
            int font = DevPanelTheme.FontSizeTable;
            float content = 0f;

            switch (column.Kind)
            {
                case DevCellKind.Label:
                    for (int i = 0; i < rows.Count; i++)
                    {
                        content = Mathf.Max(content, DevPanelBuilder.MeasureWidth(names[rows[i]], font));
                    }
                    break;

                case DevCellKind.Text:
                    content = DevPanelTheme.TableTextMinWidth;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        string value;
                        if (TryReadQuietly(column.ReadText, rows[i], out value))
                        {
                            content = Mathf.Max(content, DevPanelBuilder.MeasureWidth(value, font));
                        }
                    }
                    break;

                case DevCellKind.Number:
                    content = DevPanelBuilder.MeasureWidth(
                        column.Integer ? DevPanelTheme.TableIntegerSample : DevPanelTheme.TableNumberSample, font);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        double value;
                        if (TryReadQuietly(column.ReadNumber, rows[i], out value))
                        {
                            content = Mathf.Max(content, DevPanelBuilder.MeasureWidth(column.FormatNumber(value), font));
                        }
                    }
                    break;

                case DevCellKind.Toggle:
                    content = Mathf.Max(
                        DevPanelBuilder.MeasureWidth(DevTableToggleCell.OnText, font),
                        DevPanelBuilder.MeasureWidth(DevTableToggleCell.OffText, font));
                    break;

                case DevCellKind.Choice:
                    for (int i = 0; i < column.Options.Length; i++)
                    {
                        content = Mathf.Max(content, DevPanelBuilder.MeasureWidth(column.Options[i], font));
                    }
                    content += DevPanelTheme.CaretGap + DevPanelTheme.CaretWidth;
                    break;

                case DevCellKind.Button:
                    content = DevPanelBuilder.MeasureWidth(column.Caption, font);
                    break;
            }

            float width = content + DevPanelTheme.TableCellPad * 2f + DevPanelTheme.TableMeasureSlack;
            float header = DevPanelBuilder.MeasureWidth(column.Header, DevPanelTheme.FontSizeTableHeader)
                + DevPanelTheme.TableHeaderPad * 2f + DevPanelTheme.TableMeasureSlack;
            width = Mathf.Max(width, header);

            float floor = column.IsInteractive ? DevPanelTheme.TouchTarget : 0f;

            DevTableSize size;
            size.Preferred = Mathf.Ceil(Mathf.Max(width, floor));
            size.Min = Mathf.Ceil(Mathf.Max(size.Preferred * DevPanelTheme.TableMinShare, floor));
            // Names and buttons hold their width; the cells you type into or pick from take the slack.
            size.Flexible = column.Kind == DevCellKind.Label || column.Kind == DevCellKind.Button ? 0f : 1f;
            return size;
        }

        static void BuildHeader(Transform grid, List<DevTableColumn> columns, DevTableSize[] sizes)
        {
            RectTransform header = DevPanelBuilder.NewRect("Header", grid);
            DevPanelBuilder.AddHorizontal(header.gameObject, DevPanelTheme.TableCellGap, new RectOffset(0, 0, 0, 0));
            DevPanelBuilder.SetLayout(header.gameObject, -1f, -1f,
                DevPanelTheme.TableHeaderHeight, DevPanelTheme.TableHeaderHeight, -1f, -1f);

            for (int c = 0; c < columns.Count; c++)
            {
                DevTableColumn column = columns[c];
                RectTransform cell = DevPanelBuilder.NewRect(column.Name, header);
                sizes[c].Apply(cell.gameObject, DevPanelTheme.TableHeaderHeight);
                DevPanelBuilder.Clip(cell.gameObject);

                // Left-aligned columns keep their header over the text, not over the padding.
                bool left = IsLeftAligned(column.Kind);
                float inset = left ? DevPanelTheme.TableCellPad : DevPanelTheme.TableHeaderPad;

                Text text = DevPanelBuilder.NewText("Text", cell, column.Header, DevPanelTheme.FontSizeTableHeader,
                    DevPanelTheme.TextDim, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                DevPanelBuilder.Fill(text.rectTransform, inset, 0f, inset, 0f);
            }
        }

        static bool IsLeftAligned(DevCellKind kind)
        {
            return kind == DevCellKind.Label || kind == DevCellKind.Text || kind == DevCellKind.Choice;
        }

        void BuildRow(Transform grid, int row, string name, DevTableSize[] sizes)
        {
            List<DevTableColumn> columns = _entry.Table.Columns;

            RectTransform rect = DevPanelBuilder.NewRect(string.IsNullOrEmpty(name) ? "Row " + row : name, grid);
            DevPanelBuilder.AddHorizontal(rect.gameObject, DevPanelTheme.TableCellGap, new RectOffset(0, 0, 0, 0));
            DevPanelBuilder.SetLayout(rect.gameObject, -1f, -1f, DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, -1f, -1f);

            for (int c = 0; c < columns.Count; c++)
            {
                _cells.Add(DevTableCell.Create(this, columns[c], row, rect, sizes[c], name));
            }
        }

        void BuildAction(Transform grid, DevTableAction action)
        {
            Text label;
            Button button = DevPanelBuilder.NewButton(action.Label, grid, action.Label,
                action.Confirm ? DevPanelTheme.Danger : DevPanelTheme.Row,
                DevPanelTheme.FontSizeTable, DevPanelTheme.TextPrimary, out label);
            DevPanelBuilder.SetLayout(button.gameObject, -1f, -1f, DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, -1f, -1f);

            DevTableAction captured = action;
            button.onClick.AddListener(delegate { RunAction(captured); });
        }

        void RunAction(DevTableAction action)
        {
            _host.RunAction(_entry.Category + "/" + action.Label, action.Label, delegate
            {
                try
                {
                    action.Invoke();
                }
                finally
                {
                    // Even an action that threw halfway may have changed some rows.
                    Refresh();
                }
            }, action.Confirm);
        }

        // ---------------------------------------------------------------- live

        /// <summary>Re-reads every cell. Cheap and allocation free when nothing changed.</summary>
        internal void Refresh()
        {
            if (_released)
            {
                return;
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].Refresh();
            }
        }

        /// <summary>Detaches the view from GameObjects that are being destroyed. It never draws again.</summary>
        internal void Release()
        {
            _released = true;
            _cells.Clear();
        }

        /// <summary>Runs an edit or a tap from one cell, then re-reads the whole table.</summary>
        internal void Apply(int row, DevTableColumn column, Action change)
        {
            if (_released)
            {
                return;
            }

            _host.TryRun(CellPath(row, column), change);
            Refresh();
        }

        internal void ShowChoice(RectTransform anchor, float minWidth, string[] options, int selected, Action<int> onPick)
        {
            if (!_released)
            {
                _host.ShowChoice(anchor, minWidth, options, selected, onPick);
            }
        }

        /// <summary>Names a cell in toasts and logs: <c>Category/Table/Row/Column</c>.</summary>
        internal string CellPath(int row, DevTableColumn column)
        {
            DevTableColumn names = _entry.Table.NameColumn;
            string name = names != null ? ReadName(names, row) : null;
            return _entry.Path + "/" + (string.IsNullOrEmpty(name) ? "row " + row : name) + "/" + column.Name;
        }

        // ---------------------------------------------------------------- reads

        /// <summary>
        /// Every row's name, read once per build - label cells are not polled. A name getter that
        /// throws is logged here, since no cell reads it again, and the row is shown as "!".
        /// </summary>
        static string[] ReadNames(DevActionEntry entry)
        {
            DevTable table = entry.Table;
            string[] names = new string[table.RowCount];
            DevTableColumn column = table.NameColumn;
            if (column == null)
            {
                return names;
            }

            for (int row = 0; row < names.Length; row++)
            {
                try
                {
                    names[row] = column.ReadText(row);
                }
                catch (Exception e)
                {
                    names[row] = DevTableCell.ErrorMark;
                    DevKitLog.Exception("Table '" + entry.Path + "' threw while naming row " + row + ".", e);
                }
            }
            return names;
        }

        /// <summary>A row's name for search - quiet, since it runs on every keystroke in the search box.</summary>
        static string ReadName(DevTableColumn column, int row)
        {
            string name;
            return TryReadQuietly(column.ReadText, row, out name) ? name : null;
        }

        /// <summary>
        /// A read during construction or search. Failures are not logged here: the cell that shows
        /// the value logs them once, with its path, the first time it refreshes.
        /// </summary>
        static bool TryReadQuietly<T>(Func<int, T> read, int row, out T value)
        {
            try
            {
                value = read(row);
                return true;
            }
            catch (Exception)
            {
                value = default(T);
                return false;
            }
        }
    }
}
#endif
