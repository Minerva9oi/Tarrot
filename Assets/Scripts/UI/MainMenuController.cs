using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tarot.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string title = "Tarot";
        [SerializeField] private Color textColor = new(0.9f, 0.9f, 0.88f, 1f);
        [SerializeField] private Color mutedTextColor = new(0.58f, 0.6f, 0.66f, 1f);
        [SerializeField] private Color buttonColor = new(0.045f, 0.048f, 0.06f, 0.68f);
        [SerializeField] private Color buttonHighlightColor = new(0.12f, 0.13f, 0.17f, 0.86f);

        private Font defaultFont;
        public event Action DailyReadingRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Microsoft YaHei", "Helvetica Neue", "Arial" },
                18);
            BuildMenu();
        }

        private void BuildMenu()
        {
            EnsureEventSystem();
            var canvas = CreateCanvas();
            CreateTitle(canvas.transform);
            CreateSubtitle(canvas.transform);
            CreateMenuButtons(canvas.transform);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("Event System");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Main Menu Canvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void CreateTitle(Transform parent)
        {
            var titleText = CreateText("Title", parent, title, 92, FontStyle.Normal, textColor);
            var rect = titleText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 235f);
            rect.sizeDelta = new Vector2(560f, 120f);
        }

        private void CreateSubtitle(Transform parent)
        {
            var subtitle = CreateText("Subtitle", parent, "在静默星光中，选择今天的问题。", 24, FontStyle.Normal, mutedTextColor);
            var rect = subtitle.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 157f);
            rect.sizeDelta = new Vector2(760f, 48f);
        }

        private void CreateMenuButtons(Transform parent)
        {
            const int menuItemCount = 5;
            const float buttonWidth = 360f;
            const float buttonHeight = 58f;
            const float gap = 18f;
            var startY = 58f;

            for (var index = 0; index < menuItemCount; index++)
            {
                var item = GetMenuItem(index);
                var button = CreateButton(parent, item.Label, item.Action);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, startY - index * (buttonHeight + gap));
                rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            }
        }

        private MenuItem GetMenuItem(int index)
        {
            return index switch
            {
                0 => new MenuItem("每日运势", () => DailyReadingRequested?.Invoke()),
                1 => new MenuItem("牌阵占卜", () => Debug.Log("Spread reading selected.")),
                2 => new MenuItem("占卜日记", () => Debug.Log("Journal selected.")),
                3 => new MenuItem("设置", () => Debug.Log("Settings selected.")),
                4 => new MenuItem("退出", Application.Quit),
                _ => new MenuItem(string.Empty, () => { })
            };
        }

        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = buttonColor;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHighlightColor;
            colors.pressedColor = new Color(0.18f, 0.17f, 0.18f, 0.94f);
            colors.selectedColor = buttonHighlightColor;
            colors.disabledColor = new Color(0.04f, 0.04f, 0.05f, 0.4f);
            colors.fadeDuration = 0.18f;
            button.colors = colors;

            var labelText = CreateText("Label", buttonObject.transform, label, 25, FontStyle.Normal, textColor);
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        private Text CreateText(string objectName, Transform parent, string value, int fontSize, FontStyle fontStyle, Color color)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return text;
        }

        [Serializable]
        private readonly struct MenuItem
        {
            public MenuItem(string label, UnityEngine.Events.UnityAction action)
            {
                Label = label;
                Action = action;
            }

            public string Label { get; }
            public UnityEngine.Events.UnityAction Action { get; }
        }
    }
}
