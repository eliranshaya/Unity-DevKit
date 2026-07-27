#if DEVKIT_ENABLED
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// Static explanatory text. Used when a module cannot register anything and needs to say why
    /// - the "no adapter" hint - instead of leaving an empty category behind.
    /// </summary>
    internal static class DevInfoRow
    {
        internal static void Build(Transform parent, DevActionEntry entry, string label)
        {
            Image host = DevPanelBuilder.NewImage(entry.Label, parent, DevPanelTheme.RowQuiet);

            // minHeight only, deliberately. Setting preferredHeight as well would pin the row and
            // clip a hint that wraps onto three lines.
            LayoutElement element = host.gameObject.AddComponent<LayoutElement>();
            element.minHeight = DevPanelTheme.RowHeight;
            element.flexibleWidth = 1f;

            DevPanelBuilder.AddVertical(host.gameObject,
                DevPanelTheme.Gap,
                new RectOffset((int)DevPanelTheme.PadInner, (int)DevPanelTheme.PadInner,
                               (int)DevPanelTheme.PadInner, (int)DevPanelTheme.PadInner));

            Text title = DevPanelBuilder.NewText(
                "Title", host.transform, label,
                DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary, TextAnchor.UpperLeft, true);
            title.raycastTarget = false;

            Text body = DevPanelBuilder.NewText(
                "Body", host.transform, entry.InfoText,
                DevPanelTheme.FontSizeSmall, DevPanelTheme.TextDim, TextAnchor.UpperLeft, true);
            body.raycastTarget = false;
        }
    }
}
#endif
