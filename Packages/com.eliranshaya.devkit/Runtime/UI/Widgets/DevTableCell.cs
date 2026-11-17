#if DEVKIT_ENABLED
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// One cell of a <see cref="DevTableView"/>: a single row's value in a single column.
    /// <para>
    /// <see cref="Refresh"/> runs for every cell on screen at the watch rate, so each cell keeps
    /// what it last drew and touches its graphics only when the value really changed. The unchanged
    /// case allocates nothing - and never dirties a mesh, which is what would rebuild the canvas.
    /// </para>
    /// </summary>
    internal abstract class DevTableCell
    {
        /// <summary>Drawn in place of a value whose getter threw.</summary>
        internal const string ErrorMark = "!";

        protected readonly DevTableView View;
        protected readonly DevTableColumn Column;
        protected readonly int Row;

        /// <summary>The getter threw last time. Logged once per failing streak, not four times a second.</summary>
        bool _failing;

        protected DevTableCell(DevTableView view, DevTableColumn column, int row)
        {
            View = view;
            Column = column;
            Row = row;
        }

        /// <summary>Reads the getter and redraws if the value changed.</summary>
        internal abstract void Refresh();

        internal static DevTableCell Create(DevTableView view, DevTableColumn column, int row,
            Transform parent, DevTableSize size, string rowName)
        {
            switch (column.Kind)
            {
                case DevCellKind.Label:
                    return new DevTableLabelCell(view, column, row, parent, size, rowName);
                case DevCellKind.Text:
                    return new DevTableTextCell(view, column, row, parent, size);
                case DevCellKind.Number:
                    return new DevTableNumberCell(view, column, row, parent, size);
                case DevCellKind.Toggle:
                    return new DevTableToggleCell(view, column, row, parent, size);
                case DevCellKind.Choice:
                    return new DevTableChoiceCell(view, column, row, parent, size);
                default:
                    return new DevTableButtonCell(view, column, row, parent, size);
            }
        }

        protected bool TryRead<T>(Func<int, T> read, out T value)
        {
            try
            {
                value = read(Row);
                _failing = false;
                return true;
            }
            catch (Exception e)
            {
                value = default(T);
                if (!_failing)
                {
                    _failing = true;
                    DevKitLog.Exception("Table cell '" + View.CellPath(Row, Column) + "' threw while reading.", e);
                }
                return false;
            }
        }

        /// <summary>A tappable cell: the shared button look, sized to its column.</summary>
        protected static Button NewButtonCell(Transform parent, string name, Color background, Color textColor,
            TextAnchor anchor, DevTableSize size, out Text label)
        {
            Button button = DevPanelBuilder.NewButton(name, parent, string.Empty, background,
                DevPanelTheme.FontSizeTable, textColor, out label);
            label.alignment = anchor;
            DevPanelBuilder.Fill(label.rectTransform, DevPanelTheme.TableCellPad, 0f, DevPanelTheme.TableCellPad, 0f);
            size.Apply(button.gameObject, DevPanelTheme.RowHeight);
            DevPanelBuilder.Clip(button.gameObject);
            return button;
        }

        protected static void SetText(Text text, string value)
        {
            if (!string.Equals(text.text, value, StringComparison.Ordinal))
            {
                text.text = value;
            }
        }
    }

    /// <summary>
    /// A row name. A button when the column has a click - the row's main action - and highlighted,
    /// with an accent bar and accent text, while the column's highlight getter says so.
    /// </summary>
    internal sealed class DevTableLabelCell : DevTableCell
    {
        readonly Text _text;
        readonly GameObject _mark;

        bool _drawn;
        bool _highlighted;

        internal DevTableLabelCell(DevTableView view, DevTableColumn column, int row,
            Transform parent, DevTableSize size, string name)
            : base(view, column, row)
        {
            GameObject host;
            if (column.Click != null)
            {
                Button button = NewButtonCell(parent, column.Header, DevPanelTheme.Row, DevPanelTheme.TextPrimary,
                    TextAnchor.MiddleLeft, size, out _text);
                button.onClick.AddListener(OnClick);
                host = button.gameObject;
            }
            else
            {
                Image background = DevPanelBuilder.NewImage(column.Header, parent, DevPanelTheme.RowQuiet);
                size.Apply(background.gameObject, DevPanelTheme.RowHeight);
                DevPanelBuilder.Clip(background.gameObject);

                _text = DevPanelBuilder.NewText("Label", background.transform, string.Empty,
                    DevPanelTheme.FontSizeTable, DevPanelTheme.TextPrimary);
                DevPanelBuilder.Fill(_text.rectTransform, DevPanelTheme.TableCellPad, 0f, DevPanelTheme.TableCellPad, 0f);
                host = background.gameObject;
            }

            _text.text = name ?? string.Empty;

            Image mark = DevPanelBuilder.NewImage("Mark", host.transform, DevPanelTheme.Accent);
            mark.raycastTarget = false;
            RectTransform rect = mark.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(DevPanelTheme.TableMarkWidth, 0f);
            _mark = mark.gameObject;
            _mark.SetActive(false);
        }

        void OnClick()
        {
            View.Apply(Row, Column, delegate { Column.Click(Row); });
        }

        internal override void Refresh()
        {
            if (Column.Flag == null)
            {
                return;
            }

            bool highlighted;
            if (!TryRead(Column.Flag, out highlighted))
            {
                highlighted = false;
            }

            if (_drawn && highlighted == _highlighted)
            {
                return;
            }

            _drawn = true;
            _highlighted = highlighted;
            _mark.SetActive(highlighted);
            _text.color = highlighted ? DevPanelTheme.Accent : DevPanelTheme.TextPrimary;
        }
    }

    /// <summary>A read-only value, drawn like a watch.</summary>
    internal sealed class DevTableTextCell : DevTableCell
    {
        readonly Text _text;

        internal DevTableTextCell(DevTableView view, DevTableColumn column, int row, Transform parent, DevTableSize size)
            : base(view, column, row)
        {
            Image background = DevPanelBuilder.NewImage(column.Header, parent, DevPanelTheme.Field);
            size.Apply(background.gameObject, DevPanelTheme.RowHeight);
            DevPanelBuilder.Clip(background.gameObject);

            _text = DevPanelBuilder.NewText("Value", background.transform, string.Empty,
                DevPanelTheme.FontSizeTable, DevPanelTheme.Accent);
            DevPanelBuilder.Fill(_text.rectTransform, DevPanelTheme.TableCellPad, 0f, DevPanelTheme.TableCellPad, 0f);
        }

        internal override void Refresh()
        {
            string value;
            if (!TryRead(Column.ReadText, out value))
            {
                value = ErrorMark;
            }

            SetText(_text, value ?? string.Empty);
        }
    }

    /// <summary>
    /// An editable number. Written when the edit is committed - Enter, Done on a phone keyboard, or
    /// focus moving anywhere else - never per keystroke, so a half-typed "0." is never applied.
    /// </summary>
    internal sealed class DevTableNumberCell : DevTableCell
    {
        readonly InputField _input;

        bool _drawn;
        double _value;

        /// <summary>
        /// What the field showed before the user touched it. A commit with exactly this text is
        /// focus passing through, not an edit - nothing is written, so tapping into a field and out
        /// again never turns a value into a "change".
        /// </summary>
        string _shown = string.Empty;

        internal DevTableNumberCell(DevTableView view, DevTableColumn column, int row, Transform parent, DevTableSize size)
            : base(view, column, row)
        {
            _input = DevPanelBuilder.NewInput(column.Header, parent, string.Empty,
                column.Integer ? InputField.ContentType.IntegerNumber : InputField.ContentType.DecimalNumber,
                size.Preferred, size.Min);
            size.Apply(_input.gameObject, DevPanelTheme.RowHeight);

            Text text = _input.textComponent;
            text.fontSize = DevPanelTheme.FontSizeTable;
            text.alignment = TextAnchor.MiddleCenter;
            DevPanelBuilder.Fill(text.rectTransform, DevPanelTheme.TableCellPad, 0f, DevPanelTheme.TableCellPad, 0f);

            _input.onEndEdit.AddListener(Commit);
        }

        internal override void Refresh()
        {
            // Never overwrite what someone is in the middle of typing.
            if (_input.isFocused)
            {
                return;
            }

            double value;
            if (!TryRead(Column.ReadNumber, out value))
            {
                _drawn = false;
                Show(ErrorMark);
                return;
            }

            if (_drawn && value.Equals(_value))
            {
                return;
            }

            _drawn = true;
            _value = value;
            Show(Column.FormatNumber(value));
        }

        void Show(string text)
        {
            if (string.Equals(_shown, text, StringComparison.Ordinal) &&
                string.Equals(_input.text, text, StringComparison.Ordinal))
            {
                return;
            }

            _shown = text;
            _input.SetTextWithoutNotify(text);
        }

        void Commit(string raw)
        {
            if (string.Equals(raw, _shown, StringComparison.Ordinal))
            {
                return;
            }

            // Whatever happens next, the field must end up showing the real value: the setter may
            // clamp it, refuse it, or the text may not be a number at all.
            _drawn = false;

            double value;
            if (!Column.TryParseNumber(raw, out value))
            {
                Refresh();
                return;
            }

            View.Apply(Row, Column, delegate { Column.WriteNumber(Row, value); });
        }
    }

    /// <summary>ON / OFF. A tap writes the opposite of what the getter says now.</summary>
    internal sealed class DevTableToggleCell : DevTableCell
    {
        internal const string OnText = "ON";
        internal const string OffText = "OFF";

        readonly Text _label;

        bool _drawn;
        bool _on;

        internal DevTableToggleCell(DevTableView view, DevTableColumn column, int row, Transform parent, DevTableSize size)
            : base(view, column, row)
        {
            Button button = NewButtonCell(parent, column.Header, DevPanelTheme.Field, DevPanelTheme.TextDim,
                TextAnchor.MiddleCenter, size, out _label);
            button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            View.Apply(Row, Column, delegate { Column.WriteBool(Row, !Column.ReadBool(Row)); });
        }

        internal override void Refresh()
        {
            bool on;
            if (!TryRead(Column.ReadBool, out on))
            {
                _drawn = false;
                SetText(_label, ErrorMark);
                return;
            }

            if (_drawn && on == _on)
            {
                return;
            }

            _drawn = true;
            _on = on;
            SetText(_label, on ? OnText : OffText);
            _label.color = on ? DevPanelTheme.Accent : DevPanelTheme.TextDim;
        }
    }

    /// <summary>A dropdown: the current option and an arrow. A tap opens the panel's option list against it.</summary>
    internal sealed class DevTableChoiceCell : DevTableCell
    {
        readonly RectTransform _rect;
        readonly Text _label;
        readonly float _listWidth;

        bool _drawn;
        int _index;

        internal DevTableChoiceCell(DevTableView view, DevTableColumn column, int row, Transform parent, DevTableSize size)
            : base(view, column, row)
        {
            Button button = NewButtonCell(parent, column.Header, DevPanelTheme.Field, DevPanelTheme.TextPrimary,
                TextAnchor.MiddleLeft, size, out _label);
            button.onClick.AddListener(Open);
            _rect = (RectTransform)button.transform;
            _listWidth = size.Preferred;

            // Leave the arrow its own lane so a long option clips before reaching it.
            float arrowLane = DevPanelTheme.TableCellPad + DevPanelTheme.CaretWidth + DevPanelTheme.CaretGap;
            DevPanelBuilder.Fill(_label.rectTransform, DevPanelTheme.TableCellPad, 0f, arrowLane, 0f);

            DevTriangle caret = DevPanelBuilder.NewTriangle("Arrow", button.transform, DevPanelTheme.TextDim);
            RectTransform caretRect = caret.rectTransform;
            caretRect.anchorMin = new Vector2(1f, 0.5f);
            caretRect.anchorMax = new Vector2(1f, 0.5f);
            caretRect.pivot = new Vector2(1f, 0.5f);
            caretRect.anchoredPosition = new Vector2(-DevPanelTheme.TableCellPad, 0f);
            caretRect.sizeDelta = new Vector2(DevPanelTheme.CaretWidth, DevPanelTheme.CaretHeight);
        }

        void Open()
        {
            int current;
            if (!TryRead(Column.ReadIndex, out current))
            {
                current = -1;
            }

            View.ShowChoice(_rect, _listWidth, Column.Options, current, Pick);
        }

        void Pick(int index)
        {
            View.Apply(Row, Column, delegate { Column.WriteIndex(Row, index); });
        }

        internal override void Refresh()
        {
            int index;
            if (!TryRead(Column.ReadIndex, out index))
            {
                _drawn = false;
                SetText(_label, ErrorMark);
                return;
            }

            if (_drawn && index == _index)
            {
                return;
            }

            _drawn = true;
            _index = index;
            SetText(_label, index >= 0 && index < Column.Options.Length ? Column.Options[index] : "-");
        }
    }

    /// <summary>The column's button on this row. Dimmed and ignoring taps while the enabled getter says no.</summary>
    internal sealed class DevTableButtonCell : DevTableCell
    {
        readonly Button _button;
        readonly Text _label;

        bool _drawn;
        bool _enabled;

        internal DevTableButtonCell(DevTableView view, DevTableColumn column, int row, Transform parent, DevTableSize size)
            : base(view, column, row)
        {
            _button = NewButtonCell(parent, column.Name, DevPanelTheme.Row, DevPanelTheme.TextPrimary,
                TextAnchor.MiddleCenter, size, out _label);
            _label.text = column.Caption;
            _button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            View.Apply(Row, Column, delegate { Column.Click(Row); });
        }

        internal override void Refresh()
        {
            if (Column.Flag == null)
            {
                return;
            }

            bool enabled;
            if (!TryRead(Column.Flag, out enabled))
            {
                enabled = true;
            }

            if (_drawn && enabled == _enabled)
            {
                return;
            }

            _drawn = true;
            _enabled = enabled;
            _button.interactable = enabled;
            _label.color = enabled ? DevPanelTheme.TextPrimary : DevPanelTheme.TextDim;
        }
    }
}
#endif
