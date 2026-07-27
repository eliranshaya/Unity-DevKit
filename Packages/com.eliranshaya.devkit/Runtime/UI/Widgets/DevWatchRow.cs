#if DEVKIT_ENABLED
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// A read-only label whose right hand side is refreshed from a getter. Instances are held by
    /// the panel only while their category is on screen, so an off-screen watch costs nothing.
    /// </summary>
    internal sealed class DevWatchRow
    {
        readonly DevActionEntry _entry;
        readonly Text _value;

        string _last;

        DevWatchRow(DevActionEntry entry, Text value)
        {
            _entry = entry;
            _value = value;
            _last = null;
        }

        internal static DevWatchRow Build(Transform parent, DevActionEntry entry, string label)
        {
            RectTransform row = DevPanelBuilder.NewRect(entry.Label, parent);
            DevPanelBuilder.AddHorizontal(row.gameObject, DevPanelTheme.Gap, new RectOffset(0, 0, 0, 0));
            DevPanelBuilder.SetSize(row.gameObject, 0f, DevPanelTheme.RowHeight, 1f);

            Image labelHost = DevPanelBuilder.NewImage("Label", row, DevPanelTheme.RowQuiet);
            DevPanelBuilder.SetLayout(labelHost.gameObject,
                DevPanelTheme.LabelMinWidth, DevPanelTheme.LabelWidth,
                DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, 1f, 0f);
            DevPanelBuilder.Clip(labelHost.gameObject);

            Text labelText = DevPanelBuilder.NewText(
                "Text", labelHost.transform, label,
                DevPanelTheme.FontSizeBody, DevPanelTheme.TextDim);
            DevPanelBuilder.Fill(labelText.rectTransform, DevPanelTheme.PadInner, 0f, DevPanelTheme.PadInner, 0f);

            Image valueHost = DevPanelBuilder.NewImage("Value", row, DevPanelTheme.Field);
            DevPanelBuilder.SetLayout(valueHost.gameObject,
                DevPanelTheme.WatchValueMinWidth, DevPanelTheme.WatchValueWidth,
                DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, 0f, 0f);
            // Watch values are arbitrary game data - "1080 x 2400 @ 96 dpi" outruns its box.
            DevPanelBuilder.Clip(valueHost.gameObject);

            Text valueText = DevPanelBuilder.NewText(
                "Text", valueHost.transform, "-",
                DevPanelTheme.FontSizeBody, DevPanelTheme.Accent, TextAnchor.MiddleRight);
            DevPanelBuilder.Fill(valueText.rectTransform, DevPanelTheme.PadInner, 0f, DevPanelTheme.PadInner, 0f);

            return new DevWatchRow(entry, valueText);
        }

        /// <summary>
        /// Polls the getter and writes the result. Assigning the same string still dirties the
        /// mesh, so the previous value is compared first - that is what keeps a screen full of
        /// watches from rebuilding the canvas four times a second.
        /// </summary>
        internal void Refresh(StringBuilder scratch)
        {
            if (_value == null)
            {
                return;
            }

            string current = _entry.ReadWatch(scratch);
            if (!string.Equals(current, _last, System.StringComparison.Ordinal))
            {
                _last = current;
                _value.text = current;
            }
        }
    }
}
#endif
