#if DEVKIT_ENABLED
using UnityEngine;

namespace DevKit.Internal
{
    /// <summary>
    /// Every colour, size and spacing the panel uses. Widgets never inline a literal - if a number
    /// affects how the panel looks, it lives here.
    /// </summary>
    internal static class DevPanelTheme
    {
        // ---------------------------------------------------------------- canvas

        internal const float ReferenceWidth = 1920f;
        internal const float ReferenceHeight = 1080f;
        internal const float MatchWidthOrHeight = 0.5f;

        // ---------------------------------------------------------------- colours

        internal static readonly Color Scrim = new Color(0f, 0f, 0f, 0.72f);
        internal static readonly Color WindowBackground = new Color(0.086f, 0.098f, 0.118f, 0.98f);
        internal static readonly Color HeaderBackground = new Color(0.129f, 0.149f, 0.180f, 1f);
        internal static readonly Color RailBackground = new Color(0.106f, 0.118f, 0.145f, 1f);
        internal static readonly Color PaneBackground = new Color(0.078f, 0.086f, 0.106f, 1f);
        internal static readonly Color FooterBackground = new Color(0.129f, 0.149f, 0.180f, 1f);

        internal static readonly Color Row = new Color(0.168f, 0.192f, 0.231f, 1f);
        internal static readonly Color RowSelected = new Color(0.196f, 0.412f, 0.706f, 1f);
        internal static readonly Color RowQuiet = new Color(0.129f, 0.149f, 0.180f, 1f);
        internal static readonly Color Field = new Color(0.055f, 0.063f, 0.078f, 1f);

        internal static readonly Color Accent = new Color(0.298f, 0.616f, 0.976f, 1f);
        internal static readonly Color Danger = new Color(0.816f, 0.259f, 0.259f, 1f);
        internal static readonly Color Success = new Color(0.243f, 0.686f, 0.404f, 1f);

        internal static readonly Color TextPrimary = new Color(0.925f, 0.941f, 0.961f, 1f);
        internal static readonly Color TextDim = new Color(0.596f, 0.639f, 0.702f, 1f);
        internal static readonly Color TextOnAccent = Color.white;

        // Button tint multipliers. uGUI multiplies these into the target graphic's colour.
        internal static readonly Color TintNormal = Color.white;
        internal static readonly Color TintHighlighted = new Color(1.18f, 1.18f, 1.18f, 1f);
        internal static readonly Color TintPressed = new Color(0.78f, 0.78f, 0.78f, 1f);
        internal static readonly Color TintDisabled = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        internal const float TintFadeDuration = 0.06f;

        // ---------------------------------------------------------------- type

        internal const int FontSizeTitle = 40;
        internal const int FontSizeHeading = 30;
        internal const int FontSizeBody = 26;
        internal const int FontSizeSmall = 22;

        // ---------------------------------------------------------------- metrics

        /// <summary>Minimum touch target at the reference resolution. This runs on phones.</summary>
        internal const float TouchTarget = 88f;

        internal const float RowHeight = TouchTarget;
        internal const float HeaderHeight = 104f;
        internal const float FooterHeight = 56f;

        internal const float PadOuter = 20f;
        internal const float PadInner = 14f;
        internal const float Gap = 8f;

        internal const float ScrollSensitivity = 40f;

        // ---------------------------------------------------------------- responsive layout

        /// <summary>
        /// Below this many canvas units of width, the side rail is too expensive to keep: it is
        /// swapped for a horizontal strip of category chips above the pane.
        /// <para>
        /// A 1080x2400 phone lands at ~966 units, a 1080p desktop at 1920, a portrait tablet at
        /// ~1250. Phones in portrait get the stacked layout, everything else keeps the rail.
        /// </para>
        /// </summary>
        internal const float NarrowWidthThreshold = 1200f;

        internal const float WindowMargin = 56f;
        internal const float WindowMarginNarrow = 20f;

        /// <summary>Rail width in the wide layout.</summary>
        internal const float RailWidth = 420f;

        /// <summary>Height of the category strip in the narrow layout.</summary>
        internal const float RailStripHeight = RowHeight + PadInner * 2f;

        internal const float CategoryChipMinWidth = 190f;
        internal const float CategoryChipWidth = 260f;

        // ---------------------------------------------------------------- widget sizing
        //
        // Every widget carries a min AND a preferred width. A HorizontalLayoutGroup only shrinks
        // its children down to their minimum - past that it overflows and clips. The minimums
        // below are what make a row survive the narrowest supported pane; the preferred values are
        // what it looks like when there is room.

        internal const float LabelMinWidth = 140f;
        internal const float LabelWidth = 220f;

        internal const float FieldMinWidthNumeric = 110f;
        internal const float FieldWidthNumeric = 200f;

        internal const float FieldMinWidthString = 150f;
        internal const float FieldWidthString = 300f;

        internal const float FieldMinWidthToggle = 110f;
        internal const float FieldWidthToggle = 160f;

        internal const float RunButtonMinWidth = 100f;
        internal const float RunButtonWidth = 150f;

        internal const float WatchValueMinWidth = 130f;
        internal const float WatchValueWidth = 300f;

        internal const float SearchMinWidth = 200f;
        internal const float SearchWidth = 520f;

        internal const float TitleMinWidth = 110f;
        internal const float TitleWidth = 200f;

        internal const float ConfirmBoxWidth = 900f;
        internal const float ConfirmBoxHeight = 360f;

        // ---------------------------------------------------------------- timing

        /// <summary>Watch getters are polled at 4 Hz. Never per frame.</summary>
        internal const float WatchInterval = 0.25f;

        internal const float ToastSeconds = 4f;
    }
}
#endif
