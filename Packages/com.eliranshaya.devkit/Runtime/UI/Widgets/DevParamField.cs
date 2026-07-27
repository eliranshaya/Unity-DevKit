#if DEVKIT_ENABLED
using UnityEngine;
using UnityEngine.UI;

namespace DevKit.Internal
{
    /// <summary>
    /// Renders one action parameter. Numbers and strings get an input field; bools and enums get a
    /// button that cycles - a cycling button is one tap on a phone where a dropdown is three, and
    /// it needs no extra prefab or template.
    /// </summary>
    internal static class DevParamField
    {
        internal static void Build(Transform parent, DevParam parameter)
        {
            switch (parameter.Kind)
            {
                case DevParamKind.Int:
                    BuildInput(parent, parameter, InputField.ContentType.IntegerNumber,
                        DevPanelTheme.FieldWidthNumeric, DevPanelTheme.FieldMinWidthNumeric);
                    break;

                case DevParamKind.Float:
                    BuildInput(parent, parameter, InputField.ContentType.DecimalNumber,
                        DevPanelTheme.FieldWidthNumeric, DevPanelTheme.FieldMinWidthNumeric);
                    break;

                case DevParamKind.String:
                    BuildInput(parent, parameter, InputField.ContentType.Standard,
                        DevPanelTheme.FieldWidthString, DevPanelTheme.FieldMinWidthString);
                    break;

                case DevParamKind.Bool:
                case DevParamKind.Enum:
                    BuildCycle(parent, parameter);
                    break;
            }
        }

        static void BuildInput(Transform parent, DevParam parameter, InputField.ContentType contentType,
            float preferredWidth, float minWidth)
        {
            InputField input = DevPanelBuilder.NewInput(
                parameter.Name, parent, parameter.ValueAsString(), contentType,
                preferredWidth, minWidth, parameter.Name);

            // onValueChanged rather than onEndEdit: tapping Run steals focus, and the ordering of
            // blur against click is not something to rely on. Parsing is lenient enough that a
            // half-typed "-" never clobbers anything.
            DevParam captured = parameter;
            input.onValueChanged.AddListener(delegate (string value) { captured.ParseFromString(value); });
        }

        static void BuildCycle(Transform parent, DevParam parameter)
        {
            Text label;
            Button button = DevPanelBuilder.NewButton(
                parameter.Name, parent, parameter.ValueAsString(),
                DevPanelTheme.Field, DevPanelTheme.FontSizeBody, DevPanelTheme.TextPrimary, out label);

            bool isBool = parameter.Kind == DevParamKind.Bool;
            float width = isBool ? DevPanelTheme.FieldWidthToggle : DevPanelTheme.FieldWidthString;
            float minWidth = isBool ? DevPanelTheme.FieldMinWidthToggle : DevPanelTheme.FieldMinWidthString;
            float height = DevPanelTheme.TouchTarget - DevPanelTheme.PadInner;

            DevPanelBuilder.SetLayout(button.gameObject, minWidth, width, height, height, 0f, 0f);
            DevPanelBuilder.Clip(button.gameObject);

            DevParam captured = parameter;
            Text capturedLabel = label;
            button.onClick.AddListener(delegate
            {
                captured.CycleValue();
                capturedLabel.text = captured.ValueAsString();
            });
        }
    }
}
#endif
