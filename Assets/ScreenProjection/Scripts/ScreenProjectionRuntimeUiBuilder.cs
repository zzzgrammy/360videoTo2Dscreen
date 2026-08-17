using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Turn360To2D
{
    /// <summary>Builds a compact runtime panel for the four currently active screen slots.</summary>
    public static class ScreenProjectionRuntimeUiBuilder
    {
        public static void Create(EquirectangularScreenProjector projector, ScreenProjectionBuilder builder)
        {
            if (Object.FindObjectOfType<ScreenProjectionControlPanel>() != null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject("Screen Projection Controls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject panel = CreateUiObject("Panel", canvasObject.transform, typeof(Image));
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);
            RectTransform panelTransform = panel.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0, 1);
            panelTransform.anchorMax = new Vector2(0, 1);
            panelTransform.pivot = new Vector2(0, 1);
            panelTransform.anchoredPosition = new Vector2(24, -24);
            panelTransform.sizeDelta = new Vector2(440, 542);

            float y = -28;
            CreateText(panel.transform, "Title", "360° → Open Cube calibration", font, 22, new Vector2(18, y), new Vector2(400, 32));
            y -= 45;
            InputField frontWidth = CreateRow(panel.transform, "Front", ref y, font, out InputField frontHeight);
            InputField topWidth = CreateRow(panel.transform, "Top", ref y, font, out InputField topHeight);
            InputField leftWidth = CreateRow(panel.transform, "Left", ref y, font, out InputField leftHeight);
            InputField rightWidth = CreateRow(panel.transform, "Right", ref y, font, out InputField rightHeight);
            CreateText(panel.transform, "EyeToFront", "Eye → Front (m)", font, 17, new Vector2(18, y), new Vector2(150, 28));
            InputField eyeToFrontDistance = CreateInput(panel.transform, new Vector2(180, y), font);
            y -= 52;

            ScreenProjectionControlPanel control = panel.AddComponent<ScreenProjectionControlPanel>();
            control.Initialise(projector, builder, frontWidth, frontHeight, topWidth, topHeight, leftWidth, leftHeight, rightWidth, rightHeight, eyeToFrontDistance);
            CreateButton(panel.transform, "Apply On-site Size", font, new Vector2(18, y - 12), control.ApplyOnSiteEffectiveSizePreset);
            Button createButton = CreateButton(panel.transform, "Create Open Cube", font, new Vector2(222, y - 12), control.CreateOrUpdateOpenCube);
            CreateButton(panel.transform, "Apply Scene Adjustments", font, new Vector2(18, y - 64), control.SyncManualScreenAdjustments);
            CreateButton(panel.transform, "Create Complete 6-Sided Cube", font, new Vector2(222, y - 64), control.CreateCompleteSixSidedCube);
            createButton.Select();
        }

        private static InputField CreateRow(Transform parent, string label, ref float y, Font font, out InputField height)
        {
            CreateText(parent, label, label, font, 17, new Vector2(18, y), new Vector2(90, 28));
            CreateText(parent, "W", "W", font, 14, new Vector2(110, y), new Vector2(20, 28));
            InputField width = CreateInput(parent, new Vector2(130, y), font);
            CreateText(parent, "H", "H", font, 14, new Vector2(260, y), new Vector2(20, 28));
            height = CreateInput(parent, new Vector2(280, y), font);
            y -= 52;
            return width;
        }

        private static InputField CreateInput(Transform parent, Vector2 position, Font font)
        {
            GameObject field = CreateUiObject("Dimension", parent, typeof(Image), typeof(InputField));
            field.GetComponent<Image>().color = new Color(1, 1, 1, 0.13f);
            SetRect(field, position, new Vector2(105, 30));
            InputField input = field.GetComponent<InputField>();
            Text text = CreateText(field.transform, "Text", "", font, 16, Vector2.zero, Vector2.zero);
            text.alignment = TextAnchor.MiddleCenter;
            Stretch(text.rectTransform, 6);
            input.textComponent = text;
            return input;
        }

        private static Button CreateButton(Transform parent, string title, Font font, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateUiObject(title, parent, typeof(Image), typeof(Button));
            buttonObject.GetComponent<Image>().color = new Color(0.08f, 0.45f, 0.75f, 1);
            SetRect(buttonObject, position, new Vector2(190, 44));
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            Text label = CreateText(buttonObject.transform, "Label", title, font, 16, Vector2.zero, Vector2.zero);
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform, 4);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, int size, Vector2 position, Vector2 dimensions)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.text = content;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(textObject, position, dimensions);
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject gameObject = new GameObject(name, components);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetRect(GameObject gameObject, Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
