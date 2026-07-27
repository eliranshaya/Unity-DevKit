#if DEVKIT_ENABLED
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// One invokable row. Without parameters the whole row is the button, which is the easiest
    /// thing to hit on a phone. With parameters the label shrinks to make room for the fields and
    /// a dedicated Run button.
    /// </summary>
    internal static class DevActionRow
    {
        /// <param name="label">
        /// What the row reads. Normally the entry's own label; while a search filter is active the
        /// panel passes the full path so results from different categories stay distinguishable.
        /// </param>
        internal static void Build(Transform parent, DevActionEntry entry, string label, Action<DevActionEntry> onInvoke)
        {
            RectTransform row = DevPanelBuilder.NewRect(entry.Label, parent);
            DevPanelBuilder.AddHorizontal(row.gameObject, DevPanelTheme.Gap, new RectOffset(0, 0, 0, 0));
            DevPanelBuilder.SetSize(row.gameObject, 0f, DevPanelTheme.RowHeight, 1f);

            Color accent = entry.Confirm ? DevPanelTheme.Danger : DevPanelTheme.Row;

            if (entry.Parameters.Length == 0)
            {
                Text buttonText;
                Button button = DevPanelBuilder.NewButton(
                    "Button", row, label, accent,
                    DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary, out buttonText);

                buttonText.alignment = TextAnchor.MiddleLeft;
                DevPanelBuilder.SetSize(button.gameObject, 0f, DevPanelTheme.RowHeight, 1f);

                DevActionEntry captured = entry;
                button.onClick.AddListener(delegate { onInvoke(captured); });
                return;
            }

            Image labelHost = DevPanelBuilder.NewImage("Label", row, DevPanelTheme.RowQuiet);
            DevPanelBuilder.SetLayout(labelHost.gameObject,
                DevPanelTheme.LabelMinWidth, DevPanelTheme.LabelWidth,
                DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, 1f, 0f);
            DevPanelBuilder.Clip(labelHost.gameObject);

            Text labelText = DevPanelBuilder.NewText(
                "Text", labelHost.transform, label,
                DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary);
            DevPanelBuilder.Fill(labelText.rectTransform, DevPanelTheme.PadInner, 0f, DevPanelTheme.PadInner, 0f);

            for (int i = 0; i < entry.Parameters.Length; i++)
            {
                DevParamField.Build(row, entry.Parameters[i]);
            }

            Text runLabel;
            Button run = DevPanelBuilder.NewButton(
                "Run", row, "Run", accent,
                DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary, out runLabel);
            DevPanelBuilder.SetLayout(run.gameObject,
                DevPanelTheme.RunButtonMinWidth, DevPanelTheme.RunButtonWidth,
                DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, 0f, 0f);

            DevActionEntry capturedEntry = entry;
            run.onClick.AddListener(delegate { onInvoke(capturedEntry); });
        }
    }
}
#endif
