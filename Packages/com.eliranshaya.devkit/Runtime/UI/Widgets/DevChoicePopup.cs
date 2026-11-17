#if DEVKIT_ENABLED
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// The option list a table's dropdown cell opens. Built once by the panel and reused: it drops
    /// below the cell when there is room and opens upward when there is not, scrolls when the
    /// options outgrow the screen, and closes on a pick or on any tap outside it.
    /// </summary>
    /// <remarks>
    /// Deliberately not a uGUI <see cref="Dropdown"/>. That one puts its list on a nested canvas at
    /// sorting order 30000 - underneath this panel, which sits at <see cref="short.MaxValue"/> - and
    /// needs a template hierarchy to clone. This is one more overlay on the panel's own canvas.
    /// </remarks>
    internal sealed class DevChoicePopup
    {
        readonly GameObject _root;
        readonly RectTransform _rootRect;
        readonly RectTransform _list;
        readonly RectTransform _content;
        readonly ScrollRect _scroll;
        readonly List<Button> _items = new List<Button>(12);
        readonly List<Text> _labels = new List<Text>(12);
        readonly Vector3[] _corners = new Vector3[4];

        Action<int> _onPick;

        internal bool IsOpen { get { return _root.activeSelf; } }

        internal DevChoicePopup(Transform parent)
        {
            _rootRect = DevPanelBuilder.NewRect("Choice", parent);
            DevPanelBuilder.Fill(_rootRect);
            _root = _rootRect.gameObject;

            // A tap anywhere outside the list closes it, as a native dropdown does. Transparent,
            // but still a raycast target - which also keeps that tap off whatever is underneath.
            Image catcher = DevPanelBuilder.NewImage("Catcher", _rootRect, DevPanelTheme.ChoiceCatcher);
            DevPanelBuilder.Fill(catcher.rectTransform);
            // A culled mesh is never given a draw depth, and the raycaster skips anything without one.
            catcher.canvasRenderer.cullTransparentMesh = false;
            Button close = catcher.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(Close);

            // A sibling of the catcher rather than its child: a tap on the list's own padding must
            // not bubble up to the catcher's click handler and close the list.
            _content = DevPanelBuilder.NewScrollList("List", _rootRect, DevPanelTheme.ChoiceBackground, out _scroll);
            _list = (RectTransform)_scroll.transform;
            _list.anchorMin = new Vector2(0.5f, 0.5f);
            _list.anchorMax = new Vector2(0.5f, 0.5f);
            _list.pivot = new Vector2(0f, 1f);

            Outline border = _list.gameObject.AddComponent<Outline>();
            border.effectColor = DevPanelTheme.ChoiceBorder;
            border.effectDistance = new Vector2(DevPanelTheme.ChoiceBorderWidth, -DevPanelTheme.ChoiceBorderWidth);

            _root.SetActive(false);
        }

        /// <param name="anchor">The cell that was tapped. The list opens against it.</param>
        /// <param name="minWidth">
        /// The column's natural width. On a squeezed pane the cell can be narrower than its options;
        /// the list never is.
        /// </param>
        /// <param name="selected">Index drawn as current and scrolled into view. -1 for none.</param>
        internal void Show(RectTransform anchor, float minWidth, string[] options, int selected, Action<int> onPick)
        {
            if (anchor == null || options == null || options.Length == 0)
            {
                return;
            }

            _onPick = onPick;

            for (int i = 0; i < options.Length; i++)
            {
                if (i == _items.Count)
                {
                    AddItem();
                }

                _items[i].gameObject.SetActive(true);
                _labels[i].text = options[i];
                _items[i].targetGraphic.color = i == selected ? DevPanelTheme.RowSelected : DevPanelTheme.Row;
            }

            for (int i = options.Length; i < _items.Count; i++)
            {
                _items[i].gameObject.SetActive(false);
            }

            _root.SetActive(true);
            _rootRect.SetAsLastSibling();

            float visible = Place(anchor, minWidth, options.Length);
            Reveal(selected, options.Length, visible);
        }

        internal void Close()
        {
            _onPick = null;
            if (_root.activeSelf)
            {
                _root.SetActive(false);
            }
        }

        void AddItem()
        {
            int index = _items.Count;

            Text label;
            Button item = DevPanelBuilder.NewButton("Option " + index, _content, string.Empty,
                DevPanelTheme.Row, DevPanelTheme.FontSizeTable, DevPanelTheme.TextPrimary, out label);
            label.alignment = TextAnchor.MiddleLeft;
            DevPanelBuilder.Fill(label.rectTransform, DevPanelTheme.TableCellPad, 0f, DevPanelTheme.TableCellPad, 0f);
            DevPanelBuilder.SetSize(item.gameObject, 0f, DevPanelTheme.RowHeight, 1f);
            DevPanelBuilder.Clip(item.gameObject);

            item.onClick.AddListener(delegate { Pick(index); });

            _items.Add(item);
            _labels.Add(label);
        }

        void Pick(int index)
        {
            Action<int> callback = _onPick;
            Close();
            if (callback != null)
            {
                callback(index);
            }
        }

        /// <summary>
        /// Sizes the list and puts it against the cell: below when the options fit there, otherwise
        /// on whichever side has more room, scrolling if even that is too short. Returns the height
        /// it ended up with.
        /// </summary>
        float Place(RectTransform anchor, float minWidth, int count)
        {
            Rect bounds = _rootRect.rect;
            anchor.GetWorldCorners(_corners);
            Vector2 cellMin = _rootRect.InverseTransformPoint(_corners[0]);
            Vector2 cellMax = _rootRect.InverseTransformPoint(_corners[2]);

            float margin = DevPanelTheme.PadOuter;
            float width = Mathf.Min(Mathf.Max(cellMax.x - cellMin.x, minWidth), bounds.width - margin * 2f);
            float wanted = ContentHeight(count);

            float below = cellMin.y - DevPanelTheme.ChoiceOffset - (bounds.yMin + margin);
            float above = (bounds.yMax - margin) - (cellMax.y + DevPanelTheme.ChoiceOffset);
            bool down = wanted <= below || below >= above;
            float height = Mathf.Max(DevPanelTheme.RowHeight, Mathf.Min(wanted, down ? below : above));

            float top = down
                ? cellMin.y - DevPanelTheme.ChoiceOffset
                : cellMax.y + DevPanelTheme.ChoiceOffset + height;
            float left = Mathf.Clamp(cellMin.x, bounds.xMin + margin, bounds.xMax - margin - width);

            // Anchored at the root's centre, so a position in the root's space is offset by that centre.
            _list.sizeDelta = new Vector2(width, height);
            _list.anchoredPosition = new Vector2(left, top) - bounds.center;
            return height;
        }

        /// <summary>Scrolls so the current option sits mid-list - or at the top, when there is no current one.</summary>
        void Reveal(int selected, int count, float visible)
        {
            // The content's own size only updates on the next layout pass; the scroll maths below
            // needs it now.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scroll.StopMovement();

            float scrollable = ContentHeight(count) - visible;
            if (selected < 0 || scrollable <= 0f)
            {
                _scroll.verticalNormalizedPosition = 1f;
                return;
            }

            float itemCentre = DevPanelTheme.PadInner
                + selected * (DevPanelTheme.RowHeight + DevPanelTheme.Gap)
                + DevPanelTheme.RowHeight * 0.5f;
            float offset = Mathf.Clamp(itemCentre - visible * 0.5f, 0f, scrollable);
            _scroll.verticalNormalizedPosition = 1f - offset / scrollable;
        }

        /// <summary>Height of every option stacked, matching the padding and spacing of <see cref="DevPanelBuilder.NewScrollList"/>.</summary>
        static float ContentHeight(int count)
        {
            return count * DevPanelTheme.RowHeight
                + Mathf.Max(0, count - 1) * DevPanelTheme.Gap
                + DevPanelTheme.PadInner * 2f;
        }
    }
}
#endif
