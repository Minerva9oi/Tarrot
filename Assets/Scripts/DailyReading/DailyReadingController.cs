using System;
using System.Collections;
using System.Collections.Generic;
using Tarot.Appearance;
using Tarot.Cards;
using Tarot.RuntimeDeck;
using UnityEngine;
using UnityEngine.UI;

namespace Tarot.DailyReading
{
    public sealed class DailyReadingController : MonoBehaviour
    {
        private const int FocusIndex = 39;
        private const float CardWidth = 1.05f;
        private const float CardHeight = 1.62f;
        private const float RingRadius = 8f;
        private const float RingYOffset = -3.85f;
        private const float VisibleArcDegrees = 92f;
        private const float RotationStepDegrees = 360f / 78f;
        private const float DragSelectThreshold = 12f;

        [SerializeField] private BackgroundManager backgroundManager;
        [SerializeField] private Color cardBackColor = new(0.035f, 0.038f, 0.052f, 1f);
        [SerializeField] private Color cardFaceColor = new(0.86f, 0.84f, 0.78f, 1f);
        [SerializeField] private Color cardLineColor = new(0.78f, 0.68f, 0.48f, 1f);
        [SerializeField] private Color cardDimColor = new(0.42f, 0.44f, 0.52f, 0.88f);
        [SerializeField] private Color focusColor = new(1f, 0.92f, 0.68f, 1f);

        private readonly List<CardView> cardViews = new();
        private Font defaultFont;
        private Transform ringRoot;
        private Transform selectedAnchor;
        private Canvas canvas;
        private Text titleText;
        private Text instructionText;
        private Text resultText;
        private float rotationOffset;
        private bool isDragging;
        private bool dragExceededThreshold;
        private bool pointerDownOverUi;
        private bool isResolving;
        private Vector3 lastMousePosition;
        private Vector3 mouseDownPosition;
        private Sprite cardBackSprite;
        private Sprite cardFaceSprite;

        public event Action BackRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Microsoft YaHei", "Helvetica Neue", "Arial" },
                18);

            cardBackSprite = CreateCardSprite(cardBackColor, cardLineColor, true);
            cardFaceSprite = CreateCardSprite(cardFaceColor, new Color(0.28f, 0.24f, 0.2f, 1f), false);

            BuildScene();
            BuildDeckRing();
            SetResultVisible(false);
        }

        private void Update()
        {
            if (isResolving)
            {
                return;
            }

            HandleRotationInput();
            UpdateCardLayout();
        }

        public void SetBackgroundManager(BackgroundManager manager)
        {
            backgroundManager = manager;
        }

        private void BuildScene()
        {
            ringRoot = new GameObject("Daily Card Ring").transform;
            ringRoot.SetParent(transform, false);
            ringRoot.localPosition = Vector3.zero;

            selectedAnchor = new GameObject("Selected Card Anchor").transform;
            selectedAnchor.SetParent(transform, false);
            selectedAnchor.localPosition = new Vector3(0f, -2.15f, 0f);

            canvas = CreateCanvas();
            titleText = CreateText(canvas.transform, "每日运势", 42, new Color(0.92f, 0.9f, 0.84f, 1f), new Vector2(0f, 452f), new Vector2(520f, 70f));
            instructionText = CreateText(canvas.transform, "转动牌环，让一张牌来到正中央。点击抽取今日指引。", 22, new Color(0.66f, 0.68f, 0.74f, 1f), new Vector2(0f, 405f), new Vector2(900f, 48f));
            resultText = CreateText(canvas.transform, string.Empty, 24, new Color(0.88f, 0.86f, 0.8f, 1f), new Vector2(0f, -365f), new Vector2(980f, 180f));

            CreateButton(canvas.transform, "返回", new Vector2(-790f, -456f), () => BackRequested?.Invoke());
            CreateButton(canvas.transform, "再抽一次", new Vector2(790f, -456f), ResetReading);
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Daily Reading Canvas");
            canvasObject.transform.SetParent(transform, false);

            var createdCanvas = canvasObject.AddComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.sortingOrder = 20;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return createdCanvas;
        }

        private void BuildDeckRing()
        {
            var cards = TarotRuntimeDeck.Cards;
            for (var index = 0; index < cards.Count; index++)
            {
                var cardObject = new GameObject($"Ring Card {index:00} {cards[index].EnglishName}");
                cardObject.transform.SetParent(ringRoot, false);

                var renderer = cardObject.AddComponent<SpriteRenderer>();
                renderer.sprite = cardBackSprite;
                renderer.sortingOrder = index;

                var view = new CardView(cards[index], cardObject.transform, renderer);
                cardViews.Add(view);
            }

            UpdateCardLayout();
        }

        private void HandleRotationInput()
        {
            var scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                rotationOffset += scroll * RotationStepDegrees;
            }

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                pointerDownOverUi = IsPointerOverUi();
                isDragging = true;
                dragExceededThreshold = false;
                mouseDownPosition = UnityEngine.Input.mousePosition;
                lastMousePosition = UnityEngine.Input.mousePosition;
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                if (!pointerDownOverUi && !dragExceededThreshold && !IsPointerOverUi())
                {
                    SelectFocusedCard();
                }
            }

            if (isDragging)
            {
                var delta = UnityEngine.Input.mousePosition - lastMousePosition;
                if (Vector3.Distance(UnityEngine.Input.mousePosition, mouseDownPosition) > DragSelectThreshold)
                {
                    dragExceededThreshold = true;
                }

                rotationOffset += delta.x * 0.08f;
                lastMousePosition = UnityEngine.Input.mousePosition;
            }
        }

        private static bool IsPointerOverUi()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }

        private void UpdateCardLayout()
        {
            for (var index = 0; index < cardViews.Count; index++)
            {
                var angle = (index - FocusIndex) * RotationStepDegrees + rotationOffset + 90f;
                var normalizedAngle = Mathf.DeltaAngle(90f, angle);
                var isVisible = Mathf.Abs(normalizedAngle) <= VisibleArcDegrees * 0.5f;
                var view = cardViews[index];
                view.Transform.gameObject.SetActive(isVisible);

                if (!isVisible || view.IsSelected)
                {
                    continue;
                }

                var radians = angle * Mathf.Deg2Rad;
                var position = new Vector3(Mathf.Cos(radians) * RingRadius, Mathf.Sin(radians) * RingRadius + RingYOffset, 0f);
                var focus = 1f - Mathf.Clamp01(Mathf.Abs(normalizedAngle) / (VisibleArcDegrees * 0.5f));
                var scale = Mathf.Lerp(0.68f, 0.95f, focus);
                var tint = Color.Lerp(cardDimColor, focusColor, focus);
                tint.a = Mathf.Lerp(0.58f, 1f, focus);

                view.Transform.localPosition = position + Vector3.up * focus * 0.18f;
                view.Transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                view.Transform.localScale = new Vector3(CardWidth * scale, CardHeight * scale, 1f);
                view.Renderer.color = tint;
                view.Renderer.sortingOrder = Mathf.RoundToInt(1000 + focus * 100f);
            }
        }

        private void SelectFocusedCard()
        {
            var focused = GetFocusedCard();
            if (focused == null)
            {
                return;
            }

            isResolving = true;
            focused.IsSelected = true;
            backgroundManager?.Awaken();
            StartCoroutine(RevealCard(focused));
        }

        private CardView GetFocusedCard()
        {
            CardView bestCard = null;
            var bestDistance = float.MaxValue;

            for (var index = 0; index < cardViews.Count; index++)
            {
                var angle = (index - FocusIndex) * RotationStepDegrees + rotationOffset + 90f;
                var distance = Mathf.Abs(Mathf.DeltaAngle(90f, angle));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCard = cardViews[index];
                }
            }

            return bestCard;
        }

        private IEnumerator RevealCard(CardView view)
        {
            var startPosition = view.Transform.localPosition;
            var startRotation = view.Transform.localRotation;
            var targetPosition = selectedAnchor.localPosition;
            var targetScale = new Vector3(CardWidth * 1.2f, CardHeight * 1.2f, 1f);
            var duration = 0.72f;
            var elapsed = 0f;

            view.Transform.gameObject.SetActive(true);
            view.Renderer.sortingOrder = 2000;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Smooth01(elapsed / duration);
                view.Transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                view.Transform.localRotation = Quaternion.Slerp(startRotation, Quaternion.identity, t);
                view.Transform.localScale = Vector3.Lerp(view.Transform.localScale, targetScale, t);
                yield return null;
            }

            var orientation = UnityEngine.Random.value > 0.5f ? TarotOrientation.Upright : TarotOrientation.Reversed;
            yield return FlipCard(view, orientation);
            ShowResult(view.Card, orientation);
            backgroundManager?.Restore();
        }

        private IEnumerator FlipCard(CardView view, TarotOrientation orientation)
        {
            var duration = 0.5f;
            var elapsed = 0f;
            var changedFace = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var width = Mathf.Abs(Mathf.Cos(progress * Mathf.PI));
                view.Transform.localScale = new Vector3(CardWidth * 1.2f * width, CardHeight * 1.2f, 1f);

                if (!changedFace && progress >= 0.5f)
                {
                    changedFace = true;
                    view.Renderer.sprite = cardFaceSprite;
                    view.Renderer.color = orientation == TarotOrientation.Upright
                        ? Color.white
                        : new Color(0.92f, 0.9f, 0.96f, 1f);
                    view.Transform.localRotation = orientation == TarotOrientation.Upright
                        ? Quaternion.identity
                        : Quaternion.Euler(0f, 0f, 180f);
                }

                yield return null;
            }

            view.Transform.localScale = new Vector3(CardWidth * 1.2f, CardHeight * 1.2f, 1f);
        }

        private void ShowResult(TarotRuntimeCard card, TarotOrientation orientation)
        {
            SetResultVisible(true);
            var orientationText = orientation == TarotOrientation.Upright ? "正位" : "逆位";
            resultText.text =
                $"{card.ChineseName} / {card.EnglishName}  ·  {orientationText}\n" +
                $"今日关键词：觉察、选择、节奏\n" +
                $"今日提醒：把注意力放回你能掌控的小事，答案会慢慢变清楚。";
        }

        private void ResetReading()
        {
            foreach (var view in cardViews)
            {
                view.IsSelected = false;
                view.Renderer.sprite = cardBackSprite;
                view.Renderer.color = cardDimColor;
            }

            resultText.text = string.Empty;
            SetResultVisible(false);
            isResolving = false;
            backgroundManager?.SetIdle();
            UpdateCardLayout();
        }

        private void SetResultVisible(bool isVisible)
        {
            resultText.gameObject.SetActive(isVisible);
        }

        private Text CreateText(Transform parent, string value, int fontSize, Color color, Vector2 position, Vector2 size)
        {
            var textObject = new GameObject(value);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.045f, 0.048f, 0.06f, 0.72f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(180f, 52f);

            var text = CreateText(buttonObject.transform, label, 22, new Color(0.88f, 0.86f, 0.8f, 1f), Vector2.zero, rect.sizeDelta);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static Sprite CreateCardSprite(Color fill, Color line, bool drawBack)
        {
            const int width = 180;
            const int height = 280;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var border = x < 8 || x >= width - 8 || y < 8 || y >= height - 8;
                    var innerBorder = x < 16 || x >= width - 16 || y < 16 || y >= height - 16;
                    var color = border || innerBorder ? line : fill;

                    if (drawBack)
                    {
                        var dx = x - width * 0.5f;
                        var dy = y - height * 0.5f;
                        var distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance > 35f && distance < 39f)
                        {
                            color = line;
                        }
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 180f);
        }

        private sealed class CardView
        {
            public CardView(TarotRuntimeCard card, Transform transform, SpriteRenderer renderer)
            {
                Card = card;
                Transform = transform;
                Renderer = renderer;
            }

            public TarotRuntimeCard Card { get; }
            public Transform Transform { get; }
            public SpriteRenderer Renderer { get; }
            public bool IsSelected { get; set; }
        }
    }
}
