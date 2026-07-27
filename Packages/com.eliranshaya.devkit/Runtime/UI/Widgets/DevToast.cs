#if DEVKIT_ENABLED
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// Transient message strip pinned to the bottom of the window. An action that throws shows up
    /// here in red; the panel itself stays open and usable.
    /// </summary>
    internal sealed class DevToast
    {
        readonly GameObject _root;
        readonly Image _background;
        readonly Text _text;

        float _hideAt;

        /// <param name="bottom">Distance from the bottom of the parent, so the strip clears the footer.</param>
        internal DevToast(Transform parent, float bottom)
        {
            _background = DevPanelBuilder.NewImage("Toast", parent, DevPanelTheme.Danger);
            _root = _background.gameObject;

            RectTransform rect = _background.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(DevPanelTheme.PadOuter, bottom);
            rect.offsetMax = new Vector2(-DevPanelTheme.PadOuter, bottom + DevPanelTheme.TouchTarget);

            _text = DevPanelBuilder.NewText(
                "Text", rect, string.Empty,
                DevPanelTheme.FontSizeSmall, DevPanelTheme.TextOnAccent, TextAnchor.MiddleLeft, true);
            DevPanelBuilder.Fill(_text.rectTransform, DevPanelTheme.PadOuter, 0f, DevPanelTheme.PadOuter, 0f);

            _root.SetActive(false);
        }

        internal void Show(string message, bool isError)
        {
            _background.color = isError ? DevPanelTheme.Danger : DevPanelTheme.Success;
            _text.text = message;
            // Unscaled: pauseWhenOpen freezes Time.time and the toast would never expire.
            _hideAt = Time.unscaledTime + DevPanelTheme.ToastSeconds;
            _root.SetActive(true);
        }

        internal void Hide()
        {
            _root.SetActive(false);
        }

        internal void Tick()
        {
            if (_root.activeSelf && Time.unscaledTime >= _hideAt)
            {
                _root.SetActive(false);
            }
        }
    }
}
#endif
