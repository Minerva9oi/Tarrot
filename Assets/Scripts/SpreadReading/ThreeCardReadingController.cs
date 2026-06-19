using System;
using System.Collections;
using System.Collections.Generic;
using Tarot.Appearance;
using Tarot.Cards;
using Tarot.Readings;
using Tarot.RuntimeDeck;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tarot.SpreadReading
{
    public sealed class ThreeCardReadingController : MonoBehaviour
    {
        private const int RequiredCards = 3;
        private const float RevealedCardScale = 1.68f;
        private const float MoveDuration = 0.58f;
        private static readonly string[] PositionNames = { "过去", "现在", "未来" };
        private static readonly Vector2[] SlotViewports =
        {
            new(0.3f, 0.36f),
            new(0.5f, 0.36f),
            new(0.7f, 0.36f)
        };

        [SerializeField] private BackgroundManager backgroundManager;
        [SerializeField] private Color cardBackColor = Color.white;
        [SerializeField] private Color cardFaceColor = new(0.86f, 0.84f, 0.78f, 1f);
        [SerializeField] private Color cardLineColor = new(0.78f, 0.68f, 0.48f, 1f);
        [SerializeField] private Color cardDimColor = new(0.42f, 0.44f, 0.52f, 0.88f);
        [SerializeField] private Color focusColor = new(1f, 0.92f, 0.68f, 1f);

        private readonly List<ThreeCardDraw> drawnCards = new();
        private Font defaultFont;
        private Canvas canvas;
        private GameObject questionPanel;
        private InputField questionInput;
        private Text[] cardResultTexts = Array.Empty<Text>();
        private Transform[] slotAnchors;
        private CardDrawDeckController deckController;
        private CardDeckArtData cardDeckArt;
        private Sprite cardBackSprite;
        private Sprite fallbackCardFaceSprite;
        private bool isResolving;

        public event Action BackRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Microsoft YaHei", "Helvetica Neue", "Arial" },
                18);
            cardDeckArt = CardDeckArtData.LoadDefault();
            cardBackSprite = cardDeckArt != null && cardDeckArt.CardBackSprite != null
                ? cardDeckArt.CardBackSprite
                : CreateCardSprite(cardBackColor, cardLineColor, true);
            fallbackCardFaceSprite = CreateCardSprite(cardFaceColor, new Color(0.28f, 0.24f, 0.2f, 1f), false);

            EnsureEventSystem();
            BuildScene();
            ApplyResponsiveLayout();
            SetDrawMode(false);
            backgroundManager?.SetIdle();
        }

        private void Update()
        {
            ApplyResponsiveLayout();
        }

        public void SetBackgroundManager(BackgroundManager manager)
        {
            backgroundManager = manager;
        }

        private void BuildScene()
        {
            slotAnchors = new Transform[RequiredCards];
            for (var index = 0; index < slotAnchors.Length; index++)
            {
                slotAnchors[index] = new GameObject($"Three Card Slot {index}").transform;
                slotAnchors[index].SetParent(transform, false);
            }

            BuildDeckController();

            canvas = CreateCanvas();
            CreateQuestionPanel(canvas.transform);
            CreateCardResultTexts(canvas.transform);

            CreateButton(canvas.transform, "返回", new Vector2(-790f, -456f), new Vector2(180f, 52f), () => BackRequested?.Invoke());
            CreateButton(canvas.transform, "再抽一次", new Vector2(790f, -456f), new Vector2(180f, 52f), ResetReading);
        }

        private void BuildDeckController()
        {
            var deckObject = new GameObject("Three Card Draw Deck");
            deckObject.transform.SetParent(transform, false);
            deckController = deckObject.AddComponent<CardDrawDeckController>();
            deckController.CardSelected += HandleCardSelected;
            deckController.Initialize(
                new CardDrawLayoutProfile(
                    1.52f,
                    6.1f,
                    -5.46f,
                    34f,
                    RevealedCardScale,
                    new Vector2(0.5f, 0.4f),
                    0.82f),
                TarotRuntimeDeck.Cards,
                cardBackSprite,
                card => cardDeckArt != null ? cardDeckArt.GetFrontSprite(card.CardId) : null,
                cardDimColor,
                focusColor);
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Three Card Canvas");
            canvasObject.transform.SetParent(transform, false);

            var createdCanvas = canvasObject.AddComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.sortingOrder = 22;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return createdCanvas;
        }

        private void CreateQuestionPanel(Transform parent)
        {
            questionPanel = new GameObject("Question Panel");
            questionPanel.transform.SetParent(parent, false);

            var panelImage = questionPanel.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.04f, 0.052f, 0.86f);

            var panelRect = questionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 20f);
            panelRect.sizeDelta = new Vector2(720f, 230f);

            var prompt = CreateText(questionPanel.transform, "你想询问什么？", 25, FontStyle.Normal, new Color(0.9f, 0.88f, 0.8f, 1f), TextAnchor.MiddleCenter);
            prompt.rectTransform.anchoredPosition = new Vector2(0f, 70f);
            prompt.rectTransform.sizeDelta = new Vector2(620f, 42f);

            questionInput = CreateInputField(questionPanel.transform);
            questionInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 12f);
            questionInput.GetComponent<RectTransform>().sizeDelta = new Vector2(590f, 48f);

            CreateButton(questionPanel.transform, "开始抽牌", new Vector2(0f, -68f), new Vector2(190f, 48f), StartReading);
        }

        private void CreateCardResultTexts(Transform parent)
        {
            cardResultTexts = new Text[RequiredCards];

            for (var index = 0; index < RequiredCards; index++)
            {
                var text = CreateText(parent, string.Empty, 20, FontStyle.Normal, new Color(0.88f, 0.86f, 0.8f, 1f), TextAnchor.UpperCenter);
                text.supportRichText = true;
                text.lineSpacing = 1.04f;
                text.rectTransform.sizeDelta = new Vector2(330f, 104f);
                text.gameObject.SetActive(false);
                cardResultTexts[index] = text;
            }
        }

        private InputField CreateInputField(Transform parent)
        {
            var inputObject = new GameObject("Question Input");
            inputObject.transform.SetParent(parent, false);

            var image = inputObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.024f, 0.032f, 0.92f);

            var input = inputObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = CreateText(inputObject.transform, string.Empty, 20, FontStyle.Normal, new Color(0.86f, 0.84f, 0.78f, 1f), TextAnchor.MiddleLeft);
            input.textComponent.rectTransform.anchorMin = Vector2.zero;
            input.textComponent.rectTransform.anchorMax = Vector2.one;
            input.textComponent.rectTransform.offsetMin = new Vector2(16f, 4f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-16f, -4f);

            var placeholder = CreateText(inputObject.transform, "可以留空，例如：这件事接下来会如何发展？", 18, FontStyle.Normal, new Color(0.48f, 0.5f, 0.54f, 0.92f), TextAnchor.MiddleLeft);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(16f, 4f);
            placeholder.rectTransform.offsetMax = new Vector2(-16f, -4f);
            input.placeholder = placeholder;
            return input;
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.045f, 0.048f, 0.06f, 0.72f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.11f, 0.13f, 0.14f, 0.9f);
            colors.pressedColor = new Color(0.16f, 0.14f, 0.11f, 0.96f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.16f;
            button.colors = colors;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = CreateText(buttonObject.transform, label, 22, FontStyle.Normal, new Color(0.9f, 0.88f, 0.8f, 1f), TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
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

        private void StartReading()
        {
            questionPanel.SetActive(false);
            ClearCardResultTexts();
            SetDrawMode(true);
        }

        private void HandleCardSelected(CardDrawCardView selected)
        {
            if (isResolving || drawnCards.Count >= RequiredCards)
            {
                return;
            }

            isResolving = true;
            deckController.SetInputEnabled(false);
            var slotIndex = drawnCards.Count;
            var orientation = UnityEngine.Random.value > 0.5f ? TarotOrientation.Upright : TarotOrientation.Reversed;
            StartCoroutine(RevealSelectedCard(selected, slotIndex, orientation));
        }

        private IEnumerator RevealSelectedCard(CardDrawCardView selected, int slotIndex, TarotOrientation orientation)
        {
            selected.Collider.enabled = false;
            selected.Renderer.sortingOrder = 2600 + slotIndex;
            var startPosition = selected.Transform.localPosition;
            var targetPosition = slotAnchors[slotIndex].localPosition;
            var startScale = selected.Transform.localScale.x;
            var elapsed = 0f;

            while (elapsed < MoveDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / MoveDuration);
                var t = 1f - Mathf.Pow(1f - progress, 3f);
                selected.Transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                selected.Transform.localRotation = Quaternion.identity;
                selected.Transform.localScale = Vector3.one * Mathf.Lerp(startScale, RevealedCardScale, t);
                yield return null;
            }

            selected.Transform.localPosition = targetPosition;
            selected.Transform.localScale = Vector3.one * RevealedCardScale;
            yield return FlipSelectedCard(selected, orientation);

            drawnCards.Add(new ThreeCardDraw(selected.Card, orientation, PositionNames[slotIndex]));
            ShowCardResult(slotIndex, drawnCards[slotIndex]);
            isResolving = false;

            if (drawnCards.Count >= RequiredCards)
            {
                deckController.SetInputEnabled(false);
                yield break;
            }

            deckController.SetInputEnabled(true);
        }

        private IEnumerator FlipSelectedCard(CardDrawCardView selected, TarotOrientation orientation)
        {
            const float flipDuration = 0.48f;
            var elapsed = 0f;
            var frontApplied = false;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / flipDuration);
                var flipWidth = progress < 0.5f
                    ? Mathf.Lerp(1f, 0.08f, Smooth01(progress * 2f))
                    : Mathf.Lerp(0.08f, 1f, Smooth01((progress - 0.5f) * 2f));

                if (!frontApplied && progress >= 0.5f)
                {
                    selected.Renderer.sprite = selected.FrontSprite != null ? selected.FrontSprite : fallbackCardFaceSprite;
                    selected.Renderer.color = orientation == TarotOrientation.Upright
                        ? Color.white
                        : new Color(0.92f, 0.9f, 0.96f, 1f);
                    selected.Transform.localRotation = orientation == TarotOrientation.Upright
                        ? Quaternion.identity
                        : Quaternion.Euler(0f, 0f, 180f);
                    frontApplied = true;
                }

                selected.Transform.localScale = new Vector3(RevealedCardScale * flipWidth, RevealedCardScale, RevealedCardScale);
                yield return null;
            }

            selected.Renderer.sprite = selected.FrontSprite != null ? selected.FrontSprite : fallbackCardFaceSprite;
            selected.Renderer.color = orientation == TarotOrientation.Upright ? Color.white : new Color(0.92f, 0.9f, 0.96f, 1f);
            selected.Transform.localRotation = orientation == TarotOrientation.Upright ? Quaternion.identity : Quaternion.Euler(0f, 0f, 180f);
            selected.Transform.localScale = Vector3.one * RevealedCardScale;
        }

        private void ShowCardResult(int slotIndex, ThreeCardDraw draw)
        {
            if (slotIndex < 0 || slotIndex >= cardResultTexts.Length)
            {
                return;
            }

            cardResultTexts[slotIndex].gameObject.SetActive(true);
            cardResultTexts[slotIndex].text = FormatDraw(draw);
        }

        private static string FormatDraw(ThreeCardDraw draw)
        {
            var orientation = draw.Orientation == TarotOrientation.Upright ? "正位" : "逆位";
            return
                $"<b>{draw.Card.ChineseName}</b>  <size=15>{orientation}</size>\n" +
                $"<size=17>{CreatePositionReading(draw)}</size>";
        }

        private static string CreatePositionReading(ThreeCardDraw draw)
        {
            var tone = draw.Orientation == TarotOrientation.Upright ? "正在顺势展开" : "需要放慢确认";
            return draw.PositionName switch
            {
                "过去" => $"{GetCardTheme(draw.Card)}曾经留下影响，它{tone}。",
                "现在" => $"{GetCardTheme(draw.Card)}是当下核心，先看清它如何牵动你。",
                "未来" => $"{GetCardTheme(draw.Card)}会继续展开，把选择权留在自己手里。",
                _ => $"{GetCardTheme(draw.Card)}正在成为这张牌的重点。"
            };
        }

        private static string GetCardTheme(TarotRuntimeCard card)
        {
            if (card.ArcanaType == ArcanaType.Major)
            {
                return card.Number switch
                {
                    0 => "新的开始",
                    1 => "行动与掌控",
                    2 => "直觉与隐藏信息",
                    3 => "滋养与创造",
                    4 => "秩序与边界",
                    5 => "学习与指引",
                    6 => "关系与选择",
                    7 => "推进与意志",
                    8 => "力量与温柔",
                    9 => "独处与洞察",
                    10 => "变化与转向",
                    11 => "公平与判断",
                    12 => "暂停与换位",
                    13 => "结束与更新",
                    14 => "调和与修复",
                    15 => "欲望与束缚",
                    16 => "冲击与重建",
                    17 => "希望与疗愈",
                    18 => "梦境与不确定",
                    19 => "明朗与生命力",
                    20 => "召唤与复盘",
                    21 => "完成与整合",
                    _ => "觉察与选择"
                };
            }

            return card.Suit switch
            {
                TarotSuit.Wands => "行动、热情与推进",
                TarotSuit.Cups => "情绪、关系与感受",
                TarotSuit.Swords => "思考、判断与沟通",
                TarotSuit.Pentacles => "现实、资源与稳定",
                _ => "当下的关键线索"
            };
        }

        private void ResetReading()
        {
            StopAllCoroutines();
            drawnCards.Clear();
            isResolving = false;
            ClearCardResultTexts();
            questionInput.text = string.Empty;
            deckController.ResetDeck();
            SetDrawMode(false);
        }

        private void ClearCardResultTexts()
        {
            foreach (var text in cardResultTexts)
            {
                if (text == null)
                {
                    continue;
                }

                text.text = string.Empty;
                text.gameObject.SetActive(false);
            }
        }

        private void SetDrawMode(bool isActive)
        {
            if (deckController != null)
            {
                deckController.gameObject.SetActive(isActive);
                deckController.SetInputEnabled(isActive);
            }

            if (questionPanel != null)
            {
                questionPanel.SetActive(!isActive);
            }

            if (!isActive)
            {
                ClearCardResultTexts();
            }
        }

        private void ApplyResponsiveLayout()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic || slotAnchors == null)
            {
                return;
            }

            for (var index = 0; index < RequiredCards; index++)
            {
                slotAnchors[index].localPosition = ViewportToLocalWorldPoint(mainCamera, SlotViewports[index]);
                if (cardResultTexts != null && index < cardResultTexts.Length && cardResultTexts[index] != null)
                {
                    var textPosition = new Vector2((SlotViewports[index].x - 0.5f) * 1920f, -376f);
                    cardResultTexts[index].rectTransform.anchoredPosition = textPosition;
                }
            }
        }

        private Vector3 ViewportToLocalWorldPoint(Camera mainCamera, Vector2 viewportPosition)
        {
            var depth = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            var worldPosition = mainCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, depth));
            worldPosition.z = 0f;
            return transform.InverseTransformPoint(worldPosition);
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

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static Sprite CreateCardSprite(Color fill, Color line, bool drawBack)
        {
            const int width = 180;
            const int height = 309;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var color = fill;
                    var isBorder = x < 5 || x >= width - 5 || y < 5 || y >= height - 5;
                    if (isBorder)
                    {
                        color = Color.Lerp(line, Color.white, 0.18f);
                    }
                    else if (drawBack)
                    {
                        var dx = x - width * 0.5f;
                        var dy = y - height * 0.5f;
                        var distance = Mathf.Sqrt(dx * dx + dy * dy);
                        var centerRing = distance > 25f && distance < 30f;
                        var verticalRay = Mathf.Abs(dx) < 1.7f && Mathf.Abs(dy) < 88f;
                        var horizontalRay = Mathf.Abs(dy) < 1.7f && Mathf.Abs(dx) < 50f;
                        color = new Color(0.04f, 0.08f, 0.16f, 1f);
                        if (centerRing || verticalRay || horizontalRay)
                        {
                            color = line;
                        }
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 180f);
        }

        private readonly struct ThreeCardDraw
        {
            public ThreeCardDraw(TarotRuntimeCard card, TarotOrientation orientation, string positionName)
            {
                Card = card;
                Orientation = orientation;
                PositionName = positionName;
            }

            public TarotRuntimeCard Card { get; }
            public TarotOrientation Orientation { get; }
            public string PositionName { get; }
        }
    }
}
