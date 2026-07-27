#if DEVKIT_ENABLED
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// Builds and owns the runtime canvas: a category rail on the left, the actions of the
    /// selected category on the right, a search box, a confirm dialog and a toast strip.
    /// <para>
    /// Constructed once, on the first toggle, then only shown and hidden. Rows for a category are
    /// built when that category is selected, so a registry of 200 actions still opens instantly.
    /// </para>
    /// </summary>
    internal sealed class DevPanel : MonoBehaviour
    {
        const string GeneralCategory = "General";

        readonly List<string> _categories = new List<string>(16);
        readonly Dictionary<string, Image> _categoryTints = new Dictionary<string, Image>(16, StringComparer.Ordinal);
        readonly List<DevActionEntry> _matches = new List<DevActionEntry>(64);
        readonly List<DevWatchRow> _watchRows = new List<DevWatchRow>(16);
        readonly StringBuilder _scratch = new StringBuilder(64);

        GameObject _content;
        RectTransform _windowRect;
        RectTransform _bodyRoot;
        RectTransform _railContent;
        RectTransform _paneContent;
        Text _footerLabel;
        DevToast _toast;
        DevConfirmDialog _confirm;

        /// <summary>True while the screen is too narrow for a side rail. See <see cref="ApplyLayout"/>.</summary>
        bool _narrow;

        string _category;
        string _filter = string.Empty;

        bool _pauseWhenOpen;
        bool _paused;
        float _previousTimeScale = 1f;

        float _watchTimer;
        int _registryVersion = -1;

        internal bool IsVisible { get { return _content != null && _content.activeSelf; } }

        // ---------------------------------------------------------------- construction

        internal static DevPanel Build(bool pauseWhenOpen)
        {
            GameObject host = DevKitScene.NewRoot("Panel");
            host.layer = 5;

            DevPanel panel = host.AddComponent<DevPanel>();
            panel._pauseWhenOpen = pauseWhenOpen;
            panel.Construct();

            return panel;
        }

        void Construct()
        {
            EnsureEventSystem();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DevPanelTheme.ReferenceWidth, DevPanelTheme.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = DevPanelTheme.MatchWidthOrHeight;

            gameObject.AddComponent<GraphicRaycaster>();

            Image scrim = DevPanelBuilder.NewImage("Scrim", transform, DevPanelTheme.Scrim);
            DevPanelBuilder.Fill(scrim.rectTransform);
            _content = scrim.gameObject;

            Image window = DevPanelBuilder.NewImage("Window", scrim.transform, DevPanelTheme.WindowBackground);
            _windowRect = window.rectTransform;

            BuildHeader(window.transform);

            // Body stays an empty container: everything inside it is thrown away and rebuilt when
            // the screen crosses the narrow/wide threshold, so it has to be safe to wipe.
            _bodyRoot = DevPanelBuilder.NewRect("Body", window.transform);

            BuildFooter(window.transform);

            _toast = new DevToast(window.transform, DevPanelTheme.FooterHeight + DevPanelTheme.PadOuter);
            _confirm = new DevConfirmDialog(scrim.transform);

            ApplyLayout(true);

            _content.SetActive(false);
        }

        void BuildHeader(Transform parent)
        {
            Image header = DevPanelBuilder.NewImage("Header", parent, DevPanelTheme.HeaderBackground);
            RectTransform rect = header.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, DevPanelTheme.HeaderHeight);

            DevPanelBuilder.AddHorizontal(header.gameObject, DevPanelTheme.Gap, new RectOffset((int)DevPanelTheme.PadOuter, (int)DevPanelTheme.PadOuter, (int)DevPanelTheme.PadInner, (int)DevPanelTheme.PadInner));

            Text title = DevPanelBuilder.NewText("Title", header.transform, "DevKit", DevPanelTheme.FontSizeTitle, DevPanelTheme.Accent);
            DevPanelBuilder.SetLayout(title.gameObject,
                DevPanelTheme.TitleMinWidth, DevPanelTheme.TitleWidth, -1f, -1f, 0f, 1f);

            // The search field absorbs the slack rather than a dedicated spacer. One fewer
            // fixed-width child competing for room is what keeps this header intact on a phone.
            InputField search = DevPanelBuilder.NewInput("Search", header.transform, string.Empty,
                InputField.ContentType.Standard, DevPanelTheme.SearchWidth, DevPanelTheme.SearchMinWidth,
                "Search actions...");
            DevPanelBuilder.SetLayout(search.gameObject, -1f, -1f, -1f, -1f, 1f, -1f);
            search.onValueChanged.AddListener(OnSearchChanged);

            Text closeLabel;
            Button close = DevPanelBuilder.NewButton("Close", header.transform, "X", DevPanelTheme.Row, DevPanelTheme.FontSizeHeading, DevPanelTheme.TextPrimary, out closeLabel);
            DevPanelBuilder.SetSize(close.gameObject, DevPanelTheme.TouchTarget, DevPanelTheme.TouchTarget);
            close.onClick.AddListener(delegate
            {
                SetVisible(false);
            });
        }

        /// <summary>
        /// Reshapes the window for the current screen. Returns true when the layout actually
        /// flipped, so the caller knows the rail and pane need repopulating.
        /// </summary>
        bool ApplyLayout(bool force)
        {
            bool narrow = CanvasReferenceWidth() < DevPanelTheme.NarrowWidthThreshold;
            if (!force && narrow == _narrow)
            {
                return false;
            }

            _narrow = narrow;

            float margin = narrow ? DevPanelTheme.WindowMarginNarrow : DevPanelTheme.WindowMargin;
            DevPanelBuilder.Fill(_windowRect, margin, margin, margin, margin);
            DevPanelBuilder.Fill(_bodyRoot, 0f, DevPanelTheme.HeaderHeight, 0f, DevPanelTheme.FooterHeight);

            _confirm.SetCompact(narrow);

            DevPanelBuilder.DestroyChildren(_bodyRoot);
            BuildBody(_bodyRoot, narrow);
            return true;
        }

        /// <summary>
        /// Wide: categories in a rail down the left, actions beside it.
        /// Narrow: categories become a horizontally scrolling strip of chips above the actions,
        /// which hands the pane the entire window width.
        /// <para>
        /// On a 1080x2400 phone that is the difference between roughly 400 and 880 usable units.
        /// A watch row cannot fit in 400 - it clips - which is exactly the bug this solves.
        /// </para>
        /// </summary>
        void BuildBody(Transform parent, bool narrow)
        {
            RectTransform layout = DevPanelBuilder.NewRect("BodyLayout", parent);
            DevPanelBuilder.Fill(layout);

            ScrollRect railScroll;
            ScrollRect paneScroll;

            if (narrow)
            {
                DevPanelBuilder.AddVertical(layout.gameObject, 0f, new RectOffset(0, 0, 0, 0));

                _railContent = DevPanelBuilder.NewScrollList(
                    "Rail", layout, DevPanelTheme.RailBackground, out railScroll, true);
                DevPanelBuilder.SetLayout(railScroll.gameObject, -1f, -1f,
                    DevPanelTheme.RailStripHeight, DevPanelTheme.RailStripHeight, 1f, 0f);

                _paneContent = DevPanelBuilder.NewScrollList(
                    "Pane", layout, DevPanelTheme.PaneBackground, out paneScroll);
                DevPanelBuilder.SetLayout(paneScroll.gameObject, -1f, -1f, -1f, -1f, 1f, 1f);
            }
            else
            {
                DevPanelBuilder.AddHorizontal(layout.gameObject, 0f, new RectOffset(0, 0, 0, 0));

                _railContent = DevPanelBuilder.NewScrollList(
                    "Rail", layout, DevPanelTheme.RailBackground, out railScroll);
                DevPanelBuilder.SetLayout(railScroll.gameObject,
                    DevPanelTheme.RailWidth, DevPanelTheme.RailWidth, -1f, -1f, 0f, 1f);

                _paneContent = DevPanelBuilder.NewScrollList(
                    "Pane", layout, DevPanelTheme.PaneBackground, out paneScroll);
                DevPanelBuilder.SetLayout(paneScroll.gameObject, -1f, -1f, -1f, -1f, 1f, 1f);
            }
        }

        /// <summary>
        /// Canvas width in reference units, mirroring CanvasScaler's MatchWidthOrHeight formula.
        /// Computed rather than read off the RectTransform because it is needed during
        /// construction, before the scaler has run a layout pass.
        /// </summary>
        static float CanvasReferenceWidth()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return DevPanelTheme.ReferenceWidth;
            }

            float logWidth = Mathf.Log(Screen.width / DevPanelTheme.ReferenceWidth, 2f);
            float logHeight = Mathf.Log(Screen.height / DevPanelTheme.ReferenceHeight, 2f);
            float weighted = Mathf.Lerp(logWidth, logHeight, DevPanelTheme.MatchWidthOrHeight);
            float scale = Mathf.Pow(2f, weighted);

            return scale > 0.0001f ? Screen.width / scale : DevPanelTheme.ReferenceWidth;
        }

        void BuildFooter(Transform parent)
        {
            Image footer = DevPanelBuilder.NewImage("Footer", parent, DevPanelTheme.FooterBackground);
            RectTransform rect = footer.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, DevPanelTheme.FooterHeight);

            _footerLabel = DevPanelBuilder.NewText("Text", footer.transform, string.Empty, DevPanelTheme.FontSizeSmall, DevPanelTheme.TextDim);
            DevPanelBuilder.Fill(_footerLabel.rectTransform, DevPanelTheme.PadOuter, 0f, DevPanelTheme.PadOuter, 0f);
        }

        /// <summary>
        /// Creates an EventSystem only when the project has none. An existing one is left strictly
        /// alone - it belongs to the game, and swapping its input module would break the game's UI.
        /// </summary>
        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || DevKitCompat.FindFirst<EventSystem>() != null)
            {
                return;
            }

            GameObject host = DevKitScene.NewRoot("EventSystem");
            host.AddComponent<EventSystem>();
            DevKitInput.AttachUIModule(host);
        }

        // ---------------------------------------------------------------- visibility

        internal void SetVisible(bool visible)
        {
            if (_content == null || _content.activeSelf == visible)
            {
                return;
            }

            _content.SetActive(visible);

            if (visible)
            {
                // The screen may have rotated or resized while the panel was hidden.
                ApplyLayout(false);
                Refresh();
                ApplyPause();
            }
            else
            {
                RestorePause();
                _confirm.Close();
                _toast.Hide();
            }
        }

        void ApplyPause()
        {
            if (!_pauseWhenOpen || _paused)
            {
                return;
            }

            // Capture whatever the game was running at. Assuming 1 would silently undo a slow
            // motion effect or a game that was already paused.
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _paused = true;
        }

        void RestorePause()
        {
            if (!_paused)
            {
                return;
            }

            Time.timeScale = _previousTimeScale;
            _paused = false;
        }

        void OnDestroy()
        {
            RestorePause();
        }

        // ---------------------------------------------------------------- content

        void Refresh()
        {
            _registryVersion = DevActionRegistry.Version;
            RebuildRail();
            RebuildPane();
            UpdateFooter();
        }

        void RebuildRail()
        {
            DevPanelBuilder.DestroyChildren(_railContent);
            _categories.Clear();
            _categoryTints.Clear();

            List<DevActionEntry> all = DevActionRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                string category = all[i].Category;
                if (!_categories.Contains(category))
                {
                    _categories.Add(category);
                }
            }

            _categories.Sort(StringComparer.OrdinalIgnoreCase);

            if (_categories.Count == 0)
            {
                _category = null;
                return;
            }

            if (_category == null || !_categories.Contains(_category))
            {
                _category = _categories[0];
            }

            for (int i = 0; i < _categories.Count; i++)
            {
                string category = _categories[i];
                Text label;
                Button button = DevPanelBuilder.NewButton(category, _railContent, category, category == _category ? DevPanelTheme.RowSelected : DevPanelTheme.Row, DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary, out label);

                if (_narrow)
                {
                    // A chip in the strip: fixed width, scrolls sideways once they overflow.
                    label.alignment = TextAnchor.MiddleCenter;
                    DevPanelBuilder.SetLayout(button.gameObject,
                        DevPanelTheme.CategoryChipMinWidth, DevPanelTheme.CategoryChipWidth,
                        DevPanelTheme.RowHeight, DevPanelTheme.RowHeight, 0f, 0f);
                }
                else
                {
                    label.alignment = TextAnchor.MiddleLeft;
                    DevPanelBuilder.SetSize(button.gameObject, 0f, DevPanelTheme.RowHeight, 1f);
                }

                DevPanelBuilder.Clip(button.gameObject);
                _categoryTints[category] = (Image)button.targetGraphic;

                string captured = category;
                button.onClick.AddListener(delegate
                {
                    SelectCategory(captured);
                });
            }
        }

        void SelectCategory(string category)
        {
            if (_category == category)
            {
                return;
            }

            Image previous;
            if (_category != null && _categoryTints.TryGetValue(_category, out previous))
            {
                previous.color = DevPanelTheme.Row;
            }

            _category = category;

            Image current;
            if (_categoryTints.TryGetValue(_category, out current))
            {
                current.color = DevPanelTheme.RowSelected;
            }

            RebuildPane();
            UpdateFooter();
        }

        void OnSearchChanged(string value)
        {
            _filter = value ?? string.Empty;
            RebuildPane();
            UpdateFooter();
        }

        /// <summary>
        /// Rebuilds only the rows currently on screen: the selected category, or - while a filter
        /// is typed - every match across all categories, labelled by full path so results from
        /// different categories stay readable.
        /// </summary>
        void RebuildPane()
        {
            DevPanelBuilder.DestroyChildren(_paneContent);
            _watchRows.Clear();
            _matches.Clear();

            bool searching = _filter.Length > 0;
            List<DevActionEntry> all = DevActionRegistry.All;

            for (int i = 0; i < all.Count; i++)
            {
                DevActionEntry entry = all[i];
                if (searching)
                {
                    if (entry.Path.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _matches.Add(entry);
                    }
                }
                else if (entry.Category == _category)
                {
                    _matches.Add(entry);
                }
            }

            _matches.Sort(CompareEntries);

            if (_matches.Count == 0)
            {
                Text empty = DevPanelBuilder.NewText("Empty", _paneContent, searching ? "No action matches \"" + _filter + "\"." : "Nothing registered yet.", DevPanelTheme.FontSizeBody, DevPanelTheme.TextDim, TextAnchor.MiddleLeft, true);
                DevPanelBuilder.SetSize(empty.gameObject, 0f, DevPanelTheme.RowHeight, 1f);
                return;
            }

            for (int i = 0; i < _matches.Count; i++)
            {
                DevActionEntry entry = _matches[i];
                string label = searching ? entry.Path : entry.Label;

                switch (entry.Kind)
                {
                    case DevEntryKind.Action:
                        DevActionRow.Build(_paneContent, entry, label, InvokeEntry);
                        break;
                    case DevEntryKind.Watch:
                        _watchRows.Add(DevWatchRow.Build(_paneContent, entry, label));
                        break;
                    case DevEntryKind.Info:
                        DevInfoRow.Build(_paneContent, entry, label);
                        break;
                }
            }

            RefreshWatches();
        }

        static int CompareEntries(DevActionEntry a, DevActionEntry b)
        {
            if (a.Order != b.Order)
            {
                return a.Order.CompareTo(b.Order);
            }

            return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
        }

        void UpdateFooter()
        {
            if (_footerLabel == null)
            {
                return;
            }

            _scratch.Length = 0;
            _scratch.Append(DevActionRegistry.All.Count).Append(" entries in ").Append(_categories.Count).Append(" categories");
            if (_filter.Length > 0)
            {
                _scratch.Append("  |  ").Append(_matches.Count).Append(" matching");
            }
            else if (_category != null)
            {
                _scratch.Append("  |  ").Append(_category);
            }

            _footerLabel.text = _scratch.ToString();
        }

        // ---------------------------------------------------------------- invocation

        void InvokeEntry(DevActionEntry entry)
        {
            if (entry.Confirm)
            {
                DevActionEntry captured = entry;
                _confirm.Ask("Run \"" + entry.Path + "\"?", delegate
                {
                    Execute(captured);
                });
                return;
            }

            Execute(entry);
        }

        /// <summary>
        /// Runs an action. Anything it throws becomes a red toast plus a full stack in the console
        /// - the panel must survive a misbehaving action, never close because of one.
        /// </summary>
        void Execute(DevActionEntry entry)
        {
            try
            {
                entry.Invoke();
                _toast.Show(entry.Label + " - ok", false);
            }
            catch (Exception e)
            {
                // Reflection wraps the real failure; showing the wrapper helps nobody.
                Exception actual = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;

                _toast.Show(entry.Path + ": " + actual.Message, true);
                DevKitLog.Exception("Action '" + entry.Path + "' threw.", actual);
            }
        }

        // ---------------------------------------------------------------- tick

        void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            _toast.Tick();

            _watchTimer += Time.unscaledDeltaTime;
            if (_watchTimer < DevPanelTheme.WatchInterval)
            {
                return;
            }

            _watchTimer = 0f;

            // Device rotation, a resized Game view, a window drag between monitors. Checked at the
            // watch rate rather than per frame - it is a screen-size comparison, not a layout pass.
            if (ApplyLayout(false))
            {
                Refresh();
                return;
            }

            // Registrations can arrive after the panel was built - a scene that loaded later, say.
            if (_registryVersion != DevActionRegistry.Version)
            {
                Refresh();
                return;
            }

            RefreshWatches();
        }

        void RefreshWatches()
        {
            for (int i = 0; i < _watchRows.Count; i++)
            {
                _watchRows[i].Refresh(_scratch);
            }
        }
    }
}
#endif