using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tarot.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float ColumnAnimationSpeed = 9f;
        private const int MainMenuCount = 5;

        [SerializeField] private string title = "Tarot";
        [SerializeField] private Color textColor = new(0.9f, 0.9f, 0.88f, 1f);
        [SerializeField] private Color mutedTextColor = new(0.58f, 0.6f, 0.66f, 1f);
        [SerializeField] private Color buttonColor = new(0.045f, 0.048f, 0.06f, 0.68f);
        [SerializeField] private Color buttonHighlightColor = new(0.12f, 0.13f, 0.17f, 0.86f);

        private readonly SpreadMenuItem[] spreadItems =
        {
            new(
                "三张牌",
                3,
                true,
                "适合：过去、现在、未来的脉络梳理",
                "用三张牌看清事情从哪里来、正在如何展开、接下来会走向哪里。",
                "过去 / 现在 / 未来"),
            new(
                "关系牌阵",
                5,
                false,
                "适合：亲密关系、合作关系、互相影响",
                "预留入口。后续会用于查看双方状态、关系核心与建议。",
                "你 / 对方 / 关系核心 / 阻碍 / 建议"),
            new(
                "选择牌阵",
                5,
                false,
                "适合：两个选项之间的取舍",
                "预留入口。后续会用于比较不同选择的机会、风险和建议。",
                "选项一 / 选项二 / 机会 / 代价 / 建议")
        };

        private Font defaultFont;
        private RectTransform titleRect;
        private RectTransform subtitleRect;
        private RectTransform mainColumn;
        private RectTransform spreadColumn;
        private RectTransform detailPanel;
        private RectTransform placeholderPanel;
        private CanvasGroup spreadGroup;
        private CanvasGroup detailGroup;
        private CanvasGroup placeholderGroup;
        private Text detailTitle;
        private Text detailMeta;
        private Text detailBody;
        private Text detailPositions;
        private Text placeholderTitle;
        private Text placeholderBody;
        private Button startSpreadButton;
        private Text startSpreadLabel;
        private MenuButton[] mainButtons;
        private MenuButton[] spreadButtons;
        private int selectedMainIndex = -1;
        private int selectedSpreadIndex;
        private bool isSpreadOpen;

        public event Action DailyReadingRequested;
        public event Action ThreeCardReadingRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Microsoft YaHei", "Helvetica Neue", "Arial" },
                18);
            BuildMenu();
            SelectHome();
        }

        private void Update()
        {
            AnimateLayout();
        }

        private void BuildMenu()
        {
            EnsureEventSystem();
            var canvas = CreateCanvas();
            CreateTitle(canvas.transform);
            CreateSubtitle(canvas.transform);
            CreateMainColumn(canvas.transform);
            CreateSpreadColumn(canvas.transform);
            CreateDetailPanel(canvas.transform);
            CreatePlaceholderPanel(canvas.transform);
        }

        private void SelectHome()
        {
            selectedMainIndex = -1;
            isSpreadOpen = false;
            SetColumnVisible(spreadGroup, false);
            SetColumnVisible(detailGroup, false);
            SetColumnVisible(placeholderGroup, false);
            RefreshMainButtons();
        }

        private void SelectMain(int index)
        {
            selectedMainIndex = index;
            isSpreadOpen = index == 1;
            RefreshMainButtons();

            if (isSpreadOpen)
            {
                selectedSpreadIndex = Mathf.Clamp(selectedSpreadIndex, 0, spreadItems.Length - 1);
                SetColumnVisible(spreadGroup, true);
                SetColumnVisible(detailGroup, true);
                SetColumnVisible(placeholderGroup, false);
                ShowSpreadDetails(selectedSpreadIndex);
                RefreshSpreadButtons();
                return;
            }

            SetColumnVisible(spreadGroup, false);
            SetColumnVisible(detailGroup, false);
            SetColumnVisible(placeholderGroup, true);
            ShowPlaceholder(index);
        }

        private void SelectSpread(int index)
        {
            selectedSpreadIndex = index;
            ShowSpreadDetails(index);
            RefreshSpreadButtons();
        }

        private void ShowSpreadDetails(int index)
        {
            var item = spreadItems[index];
            detailTitle.text = item.Title;
            detailMeta.text = $"{item.CardCount} 张牌";
            detailBody.text = $"{item.Summary}\n{item.Description}";
            detailPositions.text = $"牌位：{item.Positions}";
            startSpreadButton.gameObject.SetActive(item.IsAvailable);
            startSpreadButton.interactable = item.IsAvailable;
            startSpreadLabel.text = item.IsAvailable ? "开始抽牌" : "即将开放";
        }

        private void ShowPlaceholder(int index)
        {
            var label = GetMainMenuLabel(index);
            placeholderTitle.text = label;
            placeholderBody.text = index switch
            {
                2 => "占卜日记会在这里展开历史记录、收藏与复盘。",
                3 => "设置会在这里整理卡背、音效、背景和互动偏好。",
                _ => "这个功能还没有开放。"
            };
        }

        private void AnimateLayout()
        {
            var targetMainX = selectedMainIndex < 0 ? 0f : -690f;
            var targetSpreadX = isSpreadOpen ? -258f : -128f;
            var targetDetailX = isSpreadOpen ? 438f : 610f;
            var targetPlaceholderX = selectedMainIndex >= 0 && !isSpreadOpen ? 120f : 360f;
            var t = 1f - Mathf.Exp(-ColumnAnimationSpeed * Time.unscaledDeltaTime);

            mainColumn.anchoredPosition = Vector2.Lerp(mainColumn.anchoredPosition, new Vector2(targetMainX, -20f), t);
            spreadColumn.anchoredPosition = Vector2.Lerp(spreadColumn.anchoredPosition, new Vector2(targetSpreadX, -28f), t);
            detailPanel.anchoredPosition = Vector2.Lerp(detailPanel.anchoredPosition, new Vector2(targetDetailX, -28f), t);
            placeholderPanel.anchoredPosition = Vector2.Lerp(placeholderPanel.anchoredPosition, new Vector2(targetPlaceholderX, -28f), t);
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
            var titleText = CreateText("Title", parent, title, 124, FontStyle.Normal, textColor, TextAnchor.MiddleCenter);
            titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 354f);
            titleRect.sizeDelta = new Vector2(720f, 142f);
        }

        private void CreateSubtitle(Transform parent)
        {
            var subtitle = CreateText("Subtitle", parent, "在静默星光中，选择今天的问题。", 24, FontStyle.Normal, mutedTextColor, TextAnchor.MiddleCenter);
            subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(0f, 265f);
            subtitleRect.sizeDelta = new Vector2(760f, 48f);
        }

        private void CreateMainColumn(Transform parent)
        {
            mainColumn = CreateColumn(parent, "Main Navigation", new Vector2(0f, -20f), new Vector2(390f, 380f));
            mainButtons = new MenuButton[MainMenuCount];

            const float buttonHeight = 58f;
            const float gap = 18f;
            const float startY = 120f;
            for (var index = 0; index < MainMenuCount; index++)
            {
                var captured = index;
                var button = CreateNavButton(
                    mainColumn,
                    GetMainMenuLabel(index),
                    new Vector2(0f, startY - index * (buttonHeight + gap)),
                    new Vector2(360f, buttonHeight),
                    () => HandleMainMenuClick(captured),
                    true);
                mainButtons[index] = button;
            }
        }

        private void CreateSpreadColumn(Transform parent)
        {
            spreadColumn = CreateColumn(parent, "Spread Navigation", new Vector2(-40f, -28f), new Vector2(430f, 330f));
            spreadGroup = spreadColumn.gameObject.AddComponent<CanvasGroup>();

            spreadButtons = new MenuButton[spreadItems.Length];
            for (var index = 0; index < spreadItems.Length; index++)
            {
                var item = spreadItems[index];
                var captured = index;
                var button = CreateNavButton(
                    spreadColumn,
                    item.IsAvailable ? item.Title : $"{item.Title}  即将开放",
                    new Vector2(0f, 120f - index * 76f),
                    new Vector2(390f, 58f),
                    () => SelectSpread(captured),
                    true);

                var hover = button.Button.gameObject.AddComponent<HoverTarget>();
                hover.Initialize(() => ShowSpreadDetails(captured), () => ShowSpreadDetails(selectedSpreadIndex));
                spreadButtons[index] = button;
            }
        }

        private void CreateDetailPanel(Transform parent)
        {
            detailPanel = CreateGlassPanel(parent, "Spread Detail Panel", new Vector2(438f, -28f), new Vector2(560f, 318f));
            detailGroup = detailPanel.gameObject.AddComponent<CanvasGroup>();

            detailTitle = CreateText("Detail Title", detailPanel, string.Empty, 32, FontStyle.Bold, new Color(0.94f, 0.98f, 1f, 1f), TextAnchor.UpperLeft);
            detailTitle.rectTransform.anchoredPosition = new Vector2(0f, 108f);
            detailTitle.rectTransform.sizeDelta = new Vector2(478f, 44f);

            detailMeta = CreateText("Detail Meta", detailPanel, string.Empty, 18, FontStyle.Normal, new Color(0.76f, 0.86f, 0.9f, 0.96f), TextAnchor.UpperLeft);
            detailMeta.rectTransform.anchoredPosition = new Vector2(0f, 72f);
            detailMeta.rectTransform.sizeDelta = new Vector2(478f, 30f);

            detailBody = CreateText("Detail Body", detailPanel, string.Empty, 20, FontStyle.Normal, new Color(0.84f, 0.9f, 0.92f, 0.98f), TextAnchor.UpperLeft);
            detailBody.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            detailBody.rectTransform.sizeDelta = new Vector2(478f, 86f);

            CreatePanelLine(detailPanel, "Detail Divider", new Vector2(0f, -46f), 382f, new Color(0.84f, 0.66f, 0.34f, 0.36f));

            detailPositions = CreateText("Detail Positions", detailPanel, string.Empty, 18, FontStyle.Normal, new Color(0.86f, 0.74f, 0.45f, 0.98f), TextAnchor.UpperLeft);
            detailPositions.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            detailPositions.rectTransform.sizeDelta = new Vector2(478f, 34f);

            startSpreadButton = CreateGlassButton(detailPanel, "开始抽牌", new Vector2(0f, -120f), new Vector2(188f, 46f), () => ThreeCardReadingRequested?.Invoke());
            startSpreadLabel = startSpreadButton.GetComponentInChildren<Text>();
        }

        private void CreatePlaceholderPanel(Transform parent)
        {
            placeholderPanel = CreateGlassPanel(parent, "Placeholder Detail Panel", new Vector2(360f, -28f), new Vector2(560f, 224f));
            placeholderGroup = placeholderPanel.gameObject.AddComponent<CanvasGroup>();

            placeholderTitle = CreateText("Placeholder Title", placeholderPanel, string.Empty, 34, FontStyle.Bold, new Color(0.92f, 0.98f, 1f, 1f), TextAnchor.UpperLeft);
            placeholderTitle.rectTransform.anchoredPosition = new Vector2(0f, 62f);
            placeholderTitle.rectTransform.sizeDelta = new Vector2(482f, 46f);

            placeholderBody = CreateText("Placeholder Body", placeholderPanel, string.Empty, 22, FontStyle.Normal, new Color(0.84f, 0.9f, 0.92f, 0.98f), TextAnchor.UpperLeft);
            placeholderBody.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            placeholderBody.rectTransform.sizeDelta = new Vector2(482f, 86f);
        }

        private RectTransform CreateColumn(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var columnObject = new GameObject(name);
            columnObject.transform.SetParent(parent, false);

            var rect = columnObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private RectTransform CreateGlassPanel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var panel = CreateColumn(parent, name, position, size);

            var image = panel.gameObject.AddComponent<Image>();
            image.color = new Color(0.026f, 0.045f, 0.058f, 0.38f);
            image.raycastTarget = false;

            panel.gameObject.AddComponent<RectMask2D>();

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.78f, 0.92f, 1f, 0.46f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.015f, 0.025f, 0.48f);
            shadow.effectDistance = new Vector2(0f, -9f);

            AddPanelLayer(panel, "Bottom Shade", new Color(0.01f, 0.025f, 0.04f, 0.16f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 26f), new Vector2(-44f, 66f), 0f);
            AddPanelLayer(panel, "Glass Grain", new Color(0.72f, 0.9f, 1f, 0.055f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0f, CreateGlassNoiseSprite(), Image.Type.Tiled);
            AddPanelLayer(panel, "Top Sheen", new Color(0.88f, 0.97f, 1f, 0.13f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -15f), new Vector2(-54f, 22f), 0f);
            AddPanelLayer(panel, "Mystic Diagonal Glint", new Color(0.9f, 0.98f, 1f, 0.075f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44f, 62f), new Vector2(size.x + 42f, 13f), -12f);
            AddPanelLayer(panel, "Mystic Astro Line", new Color(0.84f, 0.66f, 0.34f, 0.14f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-122f, -86f), new Vector2(size.x * 0.56f, 2f), 0f);
            AddPanelLayer(panel, "Left Edge Glint", new Color(0.92f, 0.98f, 1f, 0.34f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1.5f, 0f), new Vector2(2.5f, -28f), 0f);
            return panel;
        }

        private static Image CreatePanelLine(Transform parent, string name, Vector2 position, float width, Color color)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            var image = lineObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = new Vector2(width, 1.4f);
            return image;
        }

        private static void AddPanelLayer(
            RectTransform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            float rotation,
            Sprite sprite = null,
            Image.Type imageType = Image.Type.Simple)
        {
            var layerObject = new GameObject(name);
            layerObject.transform.SetParent(parent, false);

            var image = layerObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            image.sprite = sprite;
            image.type = imageType;

            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private MenuButton CreateNavButton(RectTransform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool isEnabled)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = isEnabled ? buttonColor : new Color(0.035f, 0.038f, 0.046f, 0.52f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = isEnabled;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = buttonHighlightColor;
            colors.pressedColor = new Color(0.18f, 0.17f, 0.18f, 0.94f);
            colors.selectedColor = buttonHighlightColor;
            colors.disabledColor = image.color;
            colors.fadeDuration = 0.18f;
            button.colors = colors;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var accentObject = new GameObject("Accent");
            accentObject.transform.SetParent(buttonObject.transform, false);
            var accent = accentObject.AddComponent<Image>();
            accent.color = new Color(0.84f, 0.66f, 0.34f, 0f);
            accent.raycastTarget = false;
            accent.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            accent.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            accent.rectTransform.sizeDelta = new Vector2(3f, size.y - 14f);

            var labelText = CreateText("Label", buttonObject.transform, label, 24, FontStyle.Normal, isEnabled ? textColor : new Color(0.5f, 0.52f, 0.56f, 0.92f), TextAnchor.MiddleLeft);
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(28f, 0f);
            labelRect.offsetMax = new Vector2(-16f, 0f);

            return new MenuButton(button, image, accent, labelText);
        }

        private Button CreateGlassButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = CreateNavButton(parent.GetComponent<RectTransform>(), label, position, size, action, true);
            button.Image.color = new Color(0.08f, 0.095f, 0.11f, 0.72f);
            button.Label.alignment = TextAnchor.MiddleCenter;
            button.Label.rectTransform.offsetMin = Vector2.zero;
            button.Label.rectTransform.offsetMax = Vector2.zero;
            return button.Button;
        }

        private Text CreateText(string objectName, Transform parent, string value, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return text;
        }

        private void RefreshMainButtons()
        {
            for (var index = 0; index < mainButtons.Length; index++)
            {
                SetButtonSelected(mainButtons[index], index == selectedMainIndex);
            }
        }

        private void RefreshSpreadButtons()
        {
            for (var index = 0; index < spreadButtons.Length; index++)
            {
                SetButtonSelected(spreadButtons[index], index == selectedSpreadIndex);
            }
        }

        private void SetButtonSelected(MenuButton button, bool isSelected)
        {
            button.Image.color = isSelected ? new Color(0.085f, 0.105f, 0.118f, 0.9f) : buttonColor;
            button.Accent.color = isSelected ? new Color(0.84f, 0.66f, 0.34f, 0.92f) : new Color(0.84f, 0.66f, 0.34f, 0f);
        }

        private static void SetColumnVisible(CanvasGroup group, bool isVisible)
        {
            group.alpha = isVisible ? 1f : 0f;
            group.interactable = isVisible;
            group.blocksRaycasts = isVisible;
        }

        private static string GetMainMenuLabel(int index)
        {
            return index switch
            {
                0 => "每日运势",
                1 => "牌阵占卜",
                2 => "占卜日记",
                3 => "设置",
                4 => "退出",
                _ => string.Empty
            };
        }

        private void HandleMainMenuClick(int index)
        {
            if (index == 0)
            {
                SelectHome();
                DailyReadingRequested?.Invoke();
                return;
            }

            if (index == 4)
            {
                Application.Quit();
                return;
            }

            SelectMain(index);
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

        private static Sprite CreateGlassNoiseSprite()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            var seed = 0x4f1bbcdc;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    seed = seed * 1103515245 + 12345;
                    var value = ((seed >> 16) & 0xff) / 255f;
                    var alpha = value > 0.67f ? Mathf.Lerp(0.03f, 0.18f, value) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        [Serializable]
        private readonly struct SpreadMenuItem
        {
            public SpreadMenuItem(string title, int cardCount, bool isAvailable, string summary, string description, string positions)
            {
                Title = title;
                CardCount = cardCount;
                IsAvailable = isAvailable;
                Summary = summary;
                Description = description;
                Positions = positions;
            }

            public string Title { get; }
            public int CardCount { get; }
            public bool IsAvailable { get; }
            public string Summary { get; }
            public string Description { get; }
            public string Positions { get; }
        }

        private readonly struct MenuButton
        {
            public MenuButton(Button button, Image image, Image accent, Text label)
            {
                Button = button;
                Image = image;
                Accent = accent;
                Label = label;
            }

            public Button Button { get; }
            public Image Image { get; }
            public Image Accent { get; }
            public Text Label { get; }
        }

        private sealed class HoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private Action enter;
            private Action exit;

            public void Initialize(Action onEnter, Action onExit)
            {
                enter = onEnter;
                exit = onExit;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                enter?.Invoke();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                exit?.Invoke();
            }
        }
    }
}
