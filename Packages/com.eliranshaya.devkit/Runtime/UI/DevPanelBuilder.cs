#if DEVKIT_ENABLED
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// Low level uGUI construction. Everything the panel draws is built from these helpers using
    /// <see cref="Texture2D.whiteTexture"/> tinted by <see cref="Image.color"/> - no sprite, no
    /// font asset and no prefab is ever loaded from disk.
    /// </summary>
    internal static class DevPanelBuilder
    {
        /// <summary>Unity's built-in UI layer.</summary>
        const int UILayer = 5;

        static Font _font;

        internal static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = DevKitCompat.LoadBuiltinFont();
                }
                return _font;
            }
        }

        // ---------------------------------------------------------------- primitives

        internal static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        /// <summary>Anchors a rect to fill its parent, inset by the given edges.</summary>
        internal static RectTransform Fill(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        internal static Image NewImage(string name, Transform parent, Color color)
        {
            RectTransform rect = NewRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        internal static Text NewText(string name, Transform parent, string value, int fontSize, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = false)
        {
            RectTransform rect = NewRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.text = value;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        // ---------------------------------------------------------------- controls

        internal static Button NewButton(string name, Transform parent, string label, Color background,
            int fontSize, Color textColor, out Text text)
        {
            RectTransform rect = NewRect(name, parent);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = background;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = DevPanelTheme.TintNormal;
            colors.highlightedColor = DevPanelTheme.TintHighlighted;
            colors.pressedColor = DevPanelTheme.TintPressed;
            colors.selectedColor = DevPanelTheme.TintNormal;
            colors.disabledColor = DevPanelTheme.TintDisabled;
            colors.fadeDuration = DevPanelTheme.TintFadeDuration;
            button.colors = colors;

            text = NewText("Label", rect, label, fontSize, textColor, TextAnchor.MiddleCenter);
            Fill(text.rectTransform, DevPanelTheme.PadInner, 0f, DevPanelTheme.PadInner, 0f);

            return button;
        }

        /// <param name="preferredWidth">Width when there is room for it.</param>
        /// <param name="minWidth">
        /// Width it is allowed to shrink to. A layout group will not shrink a child below its
        /// minimum, it overflows instead - so this is what keeps the field inside a narrow pane.
        /// </param>
        internal static InputField NewInput(string name, Transform parent, string value,
            InputField.ContentType contentType, float preferredWidth, float minWidth, string placeholder = null)
        {
            RectTransform rect = NewRect(name, parent);

            Image background = rect.gameObject.AddComponent<Image>();
            background.color = DevPanelTheme.Field;

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = minWidth;
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = 0f;
            layout.minHeight = DevPanelTheme.TouchTarget - DevPanelTheme.PadInner;
            layout.preferredHeight = DevPanelTheme.TouchTarget - DevPanelTheme.PadInner;

            // Text longer than the field must be clipped, not painted over the neighbouring widget.
            rect.gameObject.AddComponent<RectMask2D>();

            Text text = NewText("Text", rect, string.Empty, DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary);
            Fill(text.rectTransform, DevPanelTheme.PadInner, 6f, DevPanelTheme.PadInner, 6f);
            text.supportRichText = false;

            Text hint = null;
            if (!string.IsNullOrEmpty(placeholder))
            {
                hint = NewText("Placeholder", rect, placeholder, DevPanelTheme.FontSizeBody, DevPanelTheme.TextDim);
                Fill(hint.rectTransform, DevPanelTheme.PadInner, 6f, DevPanelTheme.PadInner, 6f);
            }

            InputField input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = text;
            if (hint != null)
            {
                input.placeholder = hint;
            }
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = contentType;
            input.caretColor = DevPanelTheme.TextPrimary;
            input.customCaretColor = true;
            input.selectionColor = DevPanelTheme.Accent;
            input.text = value ?? string.Empty;

            return input;
        }

        // ---------------------------------------------------------------- layout

        internal static HorizontalLayoutGroup AddHorizontal(GameObject host, float spacing, RectOffset padding)
        {
            HorizontalLayoutGroup group = host.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;
            return group;
        }

        internal static VerticalLayoutGroup AddVertical(GameObject host, float spacing, RectOffset padding)
        {
            VerticalLayoutGroup group = host.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        internal static LayoutElement SetSize(GameObject host, float minWidth, float minHeight,
            float flexibleWidth = 0f, float flexibleHeight = 0f)
        {
            LayoutElement element = host.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = host.AddComponent<LayoutElement>();
            }
            if (minWidth > 0f)
            {
                element.minWidth = minWidth;
                element.preferredWidth = minWidth;
            }
            if (minHeight > 0f)
            {
                element.minHeight = minHeight;
                element.preferredHeight = minHeight;
            }
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        /// <summary>
        /// Full control over a child's layout envelope. Pass a negative value to leave that field
        /// untouched. Prefer this over <see cref="SetSize"/> wherever a widget has to survive both
        /// a 1920-wide desktop pane and a ~900-wide phone one: separate min and preferred widths
        /// are the whole mechanism by which a row shrinks instead of overflowing.
        /// </summary>
        internal static LayoutElement SetLayout(GameObject host,
            float minWidth = -1f, float preferredWidth = -1f,
            float minHeight = -1f, float preferredHeight = -1f,
            float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            LayoutElement element = host.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = host.AddComponent<LayoutElement>();
            }

            if (minWidth >= 0f) element.minWidth = minWidth;
            if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
            if (minHeight >= 0f) element.minHeight = minHeight;
            if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) element.flexibleHeight = flexibleHeight;

            return element;
        }

        /// <summary>Clips anything drawn outside this rect. Cheaper than a stencil Mask.</summary>
        internal static void Clip(GameObject host)
        {
            if (host.GetComponent<RectMask2D>() == null)
            {
                host.AddComponent<RectMask2D>();
            }
        }

        /// <summary>
        /// A scrolling list. Returns the content rect, which already carries a layout group and a
        /// <see cref="ContentSizeFitter"/> on the scrolling axis, so callers only have to parent
        /// rows into it.
        /// </summary>
        /// <param name="horizontal">
        /// True for the category strip used by the narrow layout: scrolls sideways and lays its
        /// children out in a row. False for the normal vertical list.
        /// </param>
        internal static RectTransform NewScrollList(string name, Transform parent, Color background,
            out ScrollRect scroll, bool horizontal = false)
        {
            RectTransform root = NewRect(name, parent);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = background;

            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = DevPanelTheme.ScrollSensitivity;

            RectTransform viewport = NewRect("Viewport", root);
            Fill(viewport);
            // RectMask2D rather than Mask: no extra graphic, no stencil buffer, cheaper overdraw.
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            RectTransform content = NewRect("Content", viewport);
            RectOffset padding = new RectOffset(
                (int)DevPanelTheme.PadInner, (int)DevPanelTheme.PadInner,
                (int)DevPanelTheme.PadInner, (int)DevPanelTheme.PadInner);

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();

            if (horizontal)
            {
                // Anchored to the left edge and free to grow rightwards.
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);

                AddHorizontal(content.gameObject, DevPanelTheme.Gap, padding);

                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            else
            {
                // Anchored to the top edge and free to grow downwards.
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);

                AddVertical(content.gameObject, DevPanelTheme.Gap, padding);

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            scroll.content = content;
            return content;
        }

        internal static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                // Destroy only takes effect at the end of the frame, and until then the layout
                // group would still measure these alongside the replacements. Deactivating first
                // takes them out of the layout immediately.
                child.SetActive(false);
                Object.Destroy(child);
            }
        }
    }
}
#endif
