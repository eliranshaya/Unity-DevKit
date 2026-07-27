#if DEVKIT_ENABLED
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// Modal yes/no prompt shown for actions registered with <c>confirm: true</c>. It covers the
    /// whole window and eats clicks, so nothing behind it can fire while it is up.
    /// </summary>
    internal sealed class DevConfirmDialog
    {
        readonly GameObject _root;
        readonly RectTransform _boxRect;
        readonly Text _message;

        Action _onConfirm;

        internal bool IsOpen { get { return _root.activeSelf; } }

        internal DevConfirmDialog(Transform parent)
        {
            Image scrim = DevPanelBuilder.NewImage("Confirm", parent, DevPanelTheme.Scrim);
            _root = scrim.gameObject;
            DevPanelBuilder.Fill(scrim.rectTransform);

            Image box = DevPanelBuilder.NewImage("Box", scrim.transform, DevPanelTheme.HeaderBackground);
            _boxRect = box.rectTransform;
            _boxRect.pivot = new Vector2(0.5f, 0.5f);
            SetCompact(false);

            DevPanelBuilder.AddVertical(box.gameObject,
                DevPanelTheme.PadOuter,
                new RectOffset((int)DevPanelTheme.PadOuter, (int)DevPanelTheme.PadOuter,
                               (int)DevPanelTheme.PadOuter, (int)DevPanelTheme.PadOuter));

            _message = DevPanelBuilder.NewText(
                "Message", box.transform, string.Empty,
                DevPanelTheme.FontSizeHeading, DevPanelTheme.TextPrimary, TextAnchor.MiddleCenter, true);
            DevPanelBuilder.SetSize(_message.gameObject, 0f, 160f, 1f, 1f);

            RectTransform buttons = DevPanelBuilder.NewRect("Buttons", box.transform);
            DevPanelBuilder.AddHorizontal(buttons.gameObject, DevPanelTheme.Gap, new RectOffset(0, 0, 0, 0));
            DevPanelBuilder.SetSize(buttons.gameObject, 0f, DevPanelTheme.TouchTarget, 1f);

            Text cancelLabel;
            Button cancel = DevPanelBuilder.NewButton(
                "Cancel", buttons, "Cancel", DevPanelTheme.Row,
                DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary, out cancelLabel);
            DevPanelBuilder.SetSize(cancel.gameObject, 0f, DevPanelTheme.TouchTarget, 1f);
            cancel.onClick.AddListener(Close);

            Text confirmLabel;
            Button confirm = DevPanelBuilder.NewButton(
                "Confirm", buttons, "Do it", DevPanelTheme.Danger,
                DevPanelTheme.FontSizeBody, DevPanelTheme.TextOnAccent, out confirmLabel);
            DevPanelBuilder.SetSize(confirm.gameObject, 0f, DevPanelTheme.TouchTarget, 1f);
            confirm.onClick.AddListener(Accept);

            _root.SetActive(false);
        }

        /// <summary>
        /// Compact stretches the box to the screen width minus a margin; the fixed 900-unit box is
        /// wider than the whole canvas on a phone, where a portrait screen is only ~966 units.
        /// </summary>
        internal void SetCompact(bool compact)
        {
            const float halfHeight = DevPanelTheme.ConfirmBoxHeight * 0.5f;

            if (compact)
            {
                float inset = DevPanelTheme.PadOuter * 2f;
                _boxRect.anchorMin = new Vector2(0f, 0.5f);
                _boxRect.anchorMax = new Vector2(1f, 0.5f);
                _boxRect.offsetMin = new Vector2(inset, -halfHeight);
                _boxRect.offsetMax = new Vector2(-inset, halfHeight);
            }
            else
            {
                _boxRect.anchorMin = new Vector2(0.5f, 0.5f);
                _boxRect.anchorMax = new Vector2(0.5f, 0.5f);
                _boxRect.sizeDelta = new Vector2(DevPanelTheme.ConfirmBoxWidth, DevPanelTheme.ConfirmBoxHeight);
                _boxRect.anchoredPosition = Vector2.zero;
            }
        }

        internal void Ask(string message, Action onConfirm)
        {
            _message.text = message;
            _onConfirm = onConfirm;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        internal void Close()
        {
            _onConfirm = null;
            _root.SetActive(false);
        }

        void Accept()
        {
            Action callback = _onConfirm;
            Close();
            if (callback != null)
            {
                callback();
            }
        }
    }
}
#endif
