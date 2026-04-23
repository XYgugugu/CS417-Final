using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public static class UIFactory
    {
        private static Font cachedDefaultFont;

        public static Font DefaultFont
        {
            get
            {
                if (cachedDefaultFont == null)
                {
                    cachedDefaultFont = LoadDefaultFont();
                }

                return cachedDefaultFont;
            }
        }

        private static Font LoadDefaultFont()
        {
            Font font = null;

            // Unity 6 built-in fallback.
            try
            {
                font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                // ignored
            }

            // Final safety fallback to an OS dynamic font.
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Helvetica Neue", "Helvetica", "Arial" }, 16);
            }

            return font;
        }

        public static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.color = color;

            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            border.transform.SetParent(panel.transform, false);
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-5f, -5f);
            borderRect.offsetMax = new Vector2(5f, 5f);
            Image borderImg = border.GetComponent<Image>();
            borderImg.color = new Color(1f, 1f, 1f, 0.05f);
            border.transform.SetAsFirstSibling();

            GameObject topBand = new GameObject("TopBand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            topBand.transform.SetParent(panel.transform, false);
            RectTransform bandRect = topBand.GetComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0f, 1f);
            bandRect.anchorMax = new Vector2(1f, 1f);
            bandRect.pivot = new Vector2(0.5f, 1f);
            bandRect.sizeDelta = new Vector2(0f, 58f);
            bandRect.anchoredPosition = Vector2.zero;
            Image bandImg = topBand.GetComponent<Image>();
            bandImg.color = new Color(1f, 1f, 1f, 0.07f);

            return panel;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor, Vector2 size, Vector2 anchoredPos)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Text text = textObj.GetComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos, Color? color = null)
        {
            GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Image image = buttonObj.GetComponent<Image>();
            image.color = color ?? new Color(0.18f, 0.45f, 0.22f, 0.95f);

            Button button = buttonObj.GetComponent<Button>();

            Text buttonText = CreateText($"{name}_Text", buttonObj.transform, label, 20, TextAnchor.MiddleCenter, size, Vector2.zero);
            buttonText.color = Color.white;

            ColorBlock cb = button.colors;
            cb.normalColor = image.color;
            cb.highlightedColor = image.color * 1.2f;
            cb.pressedColor = image.color * 0.9f;
            cb.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.75f);
            button.colors = cb;

            if (buttonObj.GetComponent<UIInteractionFeedback>() == null)
            {
                buttonObj.AddComponent<UIInteractionFeedback>();
            }

            return button;
        }

        public static Toggle CreateToggle(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = size;
            rootRect.anchoredPosition = anchoredPos;

            GameObject backgroundObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObj.transform.SetParent(root.transform, false);
            RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.sizeDelta = new Vector2(28f, 28f);
            bgRect.anchoredPosition = new Vector2(16f, 0f);
            Image bgImage = backgroundObj.GetComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkObj.transform.SetParent(backgroundObj.transform, false);
            RectTransform checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(18f, 18f);
            checkRect.anchoredPosition = Vector2.zero;
            Image checkImage = checkObj.GetComponent<Image>();
            checkImage.color = new Color(0.1f, 0.85f, 0.2f, 0.95f);

            Toggle toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            Text labelText = CreateText("Label", root.transform, label, 20, TextAnchor.MiddleLeft, new Vector2(size.x - 48f, size.y), new Vector2(32f, 0f));
            labelText.alignment = TextAnchor.MiddleLeft;

            return toggle;
        }
    }
}
