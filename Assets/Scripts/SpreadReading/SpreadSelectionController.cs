using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tarot.Appearance;
using Tarot.Readings;

namespace Tarot.SpreadReading
{
    public enum SpreadTooltipStyle
    {
        Tablecloth,
        Glass,
        Gold
    }

    public sealed class SpreadSelectionController : MonoBehaviour
    {
        private const SpreadTooltipStyle DefaultTooltipStyle = SpreadTooltipStyle.Tablecloth;

        [SerializeField] private BackgroundManager backgroundManager;
        [SerializeField] private SpreadTooltipStyle tooltipStyle = DefaultTooltipStyle;

        private readonly SpreadDefinition[] spreads =
        {
            new(
                ReadingModeId.ThreeCard,
                "三张牌",
                3,
                true,
                "适合：过去、现在、未来的脉络梳理",
                "用三张牌查看事情从哪里来、正在如何展开、接下来会走向哪里。",
                new[] { "过去", "现在", "未来" }),
            new(
                ReadingModeId.ThreeCard,
                "关系牌阵",
                5,
                false,
                "适合：亲密关系、合作关系、互相影响",
                "预留入口。后续会用于查看双方状态、关系核心与建议。",
                new[] { "你", "对方", "关系核心", "阻碍", "建议" }),
            new(
                ReadingModeId.ThreeCard,
                "选择牌阵",
                5,
                false,
                "适合：两个选项之间的取舍",
                "预留入口。后续会用于比较不同选择的机会、风险和建议。",
                new[] { "选项一", "选项二", "机会", "代价", "建议" })
        };

        private Font defaultFont;
        private Canvas canvas;
        private RectTransform tooltipRect;
        private Image tooltipImage;
        private Outline tooltipOutline;
        private Shadow tooltipShadow;
        private Text tooltipTitle;
        private Text tooltipBody;
        private Text tooltipMeta;

        public event Action BackRequested;
        public event Action ThreeCardRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Microsoft YaHei", "Helvetica Neue", "Arial" },
                18);

            BuildScene();
            backgroundManager?.SetIdle();
        }

        public void SetBackgroundManager(BackgroundManager manager)
        {
            backgroundManager = manager;
        }

        public void SetTooltipStyle(SpreadTooltipStyle style)
        {
            tooltipStyle = style;
            ApplyTooltipStyle();
        }

        private void BuildScene()
        {
            EnsureEventSystem();
            canvas = CreateCanvas();
            CreateHeader(canvas.transform);
            CreateSpreadButtons(canvas.transform);
            CreateTooltip(canvas.transform);
            CreateButton(canvas.transform, "返回", new Vector2(-790f, -456f), new Vector2(180f, 52f), () => BackRequested?.Invoke(), true);
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
            var canvasObject = new GameObject("Spread Selection Canvas");
            canvasObject.transform.SetParent(transform, false);

            var createdCanvas = canvasObject.AddComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.sortingOrder = 18;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return createdCanvas;
        }

        private void CreateHeader(Transform parent)
        {
            var title = CreateText(parent, "牌阵占卜", 54, FontStyle.Normal, new Color(0.9f, 0.88f, 0.8f, 1f), TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0f, 260f);
            title.rectTransform.sizeDelta = new Vector2(680f, 82f);

            var subtitle = CreateText(parent, "选择一个牌阵，让问题落到合适的位置。", 22, FontStyle.Normal, new Color(0.58f, 0.6f, 0.66f, 1f), TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, 203f);
            subtitle.rectTransform.sizeDelta = new Vector2(760f, 44f);
        }

        private void CreateSpreadButtons(Transform parent)
        {
            const float startY = 92f;
            const float gap = 86f;

            for (var index = 0; index < spreads.Length; index++)
            {
                var spread = spreads[index];
                var button = CreateButton(
                    parent,
                    spread.Title,
                    new Vector2(-230f, startY - index * gap),
                    new Vector2(420f, 62f),
                    () => HandleSpreadClicked(spread),
                    spread.IsAvailable);

                var hover = button.gameObject.AddComponent<SpreadHoverTarget>();
                hover.Initialize(
                    () => ShowTooltip(spread, button.GetComponent<RectTransform>()),
                    HideTooltip);

                if (!spread.IsAvailable)
                {
                    var label = button.GetComponentInChildren<Text>();
                    label.text = $"{spread.Title}  即将开放";
                }
            }
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool isEnabled)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = isEnabled
                ? new Color(0.045f, 0.048f, 0.06f, 0.72f)
                : new Color(0.035f, 0.038f, 0.046f, 0.52f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = isEnabled;
            if (isEnabled)
            {
                button.onClick.AddListener(action);
            }

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.11f, 0.13f, 0.14f, 0.9f);
            colors.pressedColor = new Color(0.16f, 0.14f, 0.11f, 0.96f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = image.color;
            colors.fadeDuration = 0.16f;
            button.colors = colors;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = CreateText(buttonObject.transform, label, 25, FontStyle.Normal, isEnabled
                ? new Color(0.9f, 0.88f, 0.8f, 1f)
                : new Color(0.5f, 0.52f, 0.56f, 0.92f), TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private void CreateTooltip(Transform parent)
        {
            var tooltipObject = new GameObject("Spread Tooltip");
            tooltipObject.transform.SetParent(parent, false);

            tooltipImage = tooltipObject.AddComponent<Image>();
            tooltipImage.raycastTarget = false;
            tooltipOutline = tooltipObject.AddComponent<Outline>();
            tooltipOutline.effectDistance = new Vector2(1.2f, -1.2f);
            tooltipShadow = tooltipObject.AddComponent<Shadow>();
            tooltipShadow.effectDistance = new Vector2(0f, -8f);

            tooltipRect = tooltipObject.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0f, 0.5f);
            tooltipRect.sizeDelta = new Vector2(470f, 174f);

            tooltipTitle = CreateText(tooltipObject.transform, string.Empty, 25, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            tooltipTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            tooltipTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            tooltipTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            tooltipTitle.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            tooltipTitle.rectTransform.sizeDelta = new Vector2(-40f, 34f);

            tooltipMeta = CreateText(tooltipObject.transform, string.Empty, 18, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            tooltipMeta.rectTransform.anchorMin = new Vector2(0f, 1f);
            tooltipMeta.rectTransform.anchorMax = new Vector2(1f, 1f);
            tooltipMeta.rectTransform.pivot = new Vector2(0.5f, 1f);
            tooltipMeta.rectTransform.anchoredPosition = new Vector2(0f, -56f);
            tooltipMeta.rectTransform.sizeDelta = new Vector2(-40f, 28f);

            tooltipBody = CreateText(tooltipObject.transform, string.Empty, 19, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            tooltipBody.rectTransform.anchorMin = new Vector2(0f, 0f);
            tooltipBody.rectTransform.anchorMax = new Vector2(1f, 1f);
            tooltipBody.rectTransform.offsetMin = new Vector2(20f, 18f);
            tooltipBody.rectTransform.offsetMax = new Vector2(-20f, -86f);

            ApplyTooltipStyle();
            tooltipObject.SetActive(false);
        }

        private void ApplyTooltipStyle()
        {
            if (tooltipImage == null)
            {
                return;
            }

            var style = GetTooltipPalette(tooltipStyle);
            tooltipImage.color = style.Background;
            tooltipOutline.effectColor = style.Border;
            tooltipShadow.effectColor = style.Shadow;
            tooltipTitle.color = style.Title;
            tooltipMeta.color = style.Meta;
            tooltipBody.color = style.Body;
        }

        private void ShowTooltip(SpreadDefinition spread, RectTransform sourceRect)
        {
            tooltipTitle.text = spread.Title;
            tooltipMeta.text = $"{spread.CardCount} 张牌 · {spread.Summary}";
            tooltipBody.text = spread.Description;
            tooltipRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(270f, 0f);
            tooltipRect.gameObject.SetActive(true);
        }

        private void HideTooltip()
        {
            if (tooltipRect != null)
            {
                tooltipRect.gameObject.SetActive(false);
            }
        }

        private void HandleSpreadClicked(SpreadDefinition spread)
        {
            if (!spread.IsAvailable)
            {
                return;
            }

            if (spread.ModeId == ReadingModeId.ThreeCard)
            {
                ThreeCardRequested?.Invoke();
            }
        }

        private Text CreateText(Transform parent, string value, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            var textObject = new GameObject("Text");
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

        private static TooltipPalette GetTooltipPalette(SpreadTooltipStyle style)
        {
            return style switch
            {
                SpreadTooltipStyle.Glass => new TooltipPalette(
                    new Color(0.05f, 0.07f, 0.09f, 0.55f),
                    new Color(0.68f, 0.82f, 0.88f, 0.36f),
                    new Color(0f, 0f, 0f, 0.34f),
                    new Color(0.9f, 0.96f, 1f, 1f),
                    new Color(0.68f, 0.78f, 0.86f, 0.96f),
                    new Color(0.82f, 0.88f, 0.92f, 0.96f)),
                SpreadTooltipStyle.Gold => new TooltipPalette(
                    new Color(0.065f, 0.048f, 0.025f, 0.9f),
                    new Color(0.92f, 0.66f, 0.28f, 0.74f),
                    new Color(0f, 0f, 0f, 0.52f),
                    new Color(1f, 0.82f, 0.42f, 1f),
                    new Color(0.86f, 0.68f, 0.38f, 0.96f),
                    new Color(0.92f, 0.86f, 0.72f, 0.96f)),
                _ => new TooltipPalette(
                    new Color(0.025f, 0.055f, 0.058f, 0.9f),
                    new Color(0.76f, 0.66f, 0.38f, 0.58f),
                    new Color(0f, 0f, 0f, 0.46f),
                    new Color(0.9f, 0.86f, 0.72f, 1f),
                    new Color(0.6f, 0.68f, 0.66f, 0.96f),
                    new Color(0.78f, 0.82f, 0.78f, 0.96f))
            };
        }

        private readonly struct TooltipPalette
        {
            public TooltipPalette(Color background, Color border, Color shadow, Color title, Color meta, Color body)
            {
                Background = background;
                Border = border;
                Shadow = shadow;
                Title = title;
                Meta = meta;
                Body = body;
            }

            public Color Background { get; }
            public Color Border { get; }
            public Color Shadow { get; }
            public Color Title { get; }
            public Color Meta { get; }
            public Color Body { get; }
        }

        private readonly struct SpreadDefinition
        {
            public SpreadDefinition(ReadingModeId modeId, string title, int cardCount, bool isAvailable, string summary, string description, string[] positions)
            {
                ModeId = modeId;
                Title = title;
                CardCount = cardCount;
                IsAvailable = isAvailable;
                Summary = summary;
                Description = description;
                Positions = positions;
            }

            public ReadingModeId ModeId { get; }
            public string Title { get; }
            public int CardCount { get; }
            public bool IsAvailable { get; }
            public string Summary { get; }
            public string Description { get; }
            public string[] Positions { get; }
        }

        private sealed class SpreadHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
