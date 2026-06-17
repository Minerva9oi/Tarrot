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
        private const float RingCardScale = 1.5f;
        private const float RingRadius = 24f;
        private const float RingYOffset = -20.65f;
        private const float VisibleArcDegrees = 68f;
        private const float SelectedCardScale = 1.45f;
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
        private CardDeckArtData cardDeckArt;
        private Sprite cardBackSprite;
        private Sprite fallbackCardFaceSprite;

        public event Action BackRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Microsoft YaHei", "Helvetica Neue", "Arial" },
                18);

            cardDeckArt = CardDeckArtData.LoadDefault();
            cardBackSprite = CreateCardSprite(cardBackColor, cardLineColor, true);
            fallbackCardFaceSprite = CreateCardSprite(cardFaceColor, new Color(0.28f, 0.24f, 0.2f, 1f), false);

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
            instructionText = CreateText(canvas.transform, "滚轮或拖拽转动牌环，点击任意可见牌抽取今日指引。", 22, new Color(0.66f, 0.68f, 0.74f, 1f), new Vector2(0f, 405f), new Vector2(900f, 48f));
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

                var collider = cardObject.AddComponent<BoxCollider2D>();
                collider.size = cardBackSprite.bounds.size;

                var frontSprite = cardDeckArt != null ? cardDeckArt.GetFrontSprite(cards[index].CardId) : null;
                var view = new CardView(cards[index], cardObject.transform, renderer, collider, frontSprite);
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
                    TrySelectClickedCard(UnityEngine.Input.mousePosition);
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
                var centerProximity = 1f - Mathf.Clamp01(Mathf.Abs(normalizedAngle) / (VisibleArcDegrees * 0.5f));
                var tint = Color.Lerp(cardDimColor, focusColor, 0.2f + centerProximity * 0.22f);
                tint.a = Mathf.Lerp(0.72f, 0.96f, centerProximity);

                view.Transform.localPosition = position;
                view.Transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                view.Transform.localScale = new Vector3(RingCardScale, RingCardScale, 1f);
                view.Renderer.color = tint;
                view.Renderer.sortingOrder = Mathf.RoundToInt(1000 + centerProximity * 100f);
            }
        }

        private void TrySelectClickedCard(Vector3 screenPosition)
        {
            var selected = GetClickedCard(screenPosition);
            if (selected == null)
            {
                return;
            }

            isResolving = true;
            selected.IsSelected = true;
            backgroundManager?.Awaken();
            StartCoroutine(RevealCard(selected));
        }

        private CardView GetClickedCard(Vector3 screenPosition)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            var worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            var hits = Physics2D.OverlapPointAll(new Vector2(worldPosition.x, worldPosition.y));
            CardView bestCard = null;
            var bestSortingOrder = int.MinValue;

            foreach (var hit in hits)
            {
                foreach (var view in cardViews)
                {
                    if (!view.Transform.gameObject.activeSelf || view.IsSelected || hit != view.Collider)
                    {
                        continue;
                    }

                    if (view.Renderer.sortingOrder > bestSortingOrder)
                    {
                        bestSortingOrder = view.Renderer.sortingOrder;
                        bestCard = view;
                    }
                }
            }

            return bestCard;
        }

        private IEnumerator RevealCard(CardView view)
        {
            var startPosition = view.Transform.localPosition;
            var startRotation = view.Transform.localRotation;
            var targetPosition = selectedAnchor.localPosition;
            var targetScale = new Vector3(SelectedCardScale, SelectedCardScale, 1f);
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
                view.Transform.localScale = new Vector3(SelectedCardScale * width, SelectedCardScale, 1f);

                if (!changedFace && progress >= 0.5f)
                {
                    changedFace = true;
                    view.Renderer.sprite = view.FrontSprite != null ? view.FrontSprite : fallbackCardFaceSprite;
                    view.Renderer.color = orientation == TarotOrientation.Upright
                        ? Color.white
                        : new Color(0.92f, 0.9f, 0.96f, 1f);
                    view.Transform.localRotation = orientation == TarotOrientation.Upright
                        ? Quaternion.identity
                        : Quaternion.Euler(0f, 0f, 180f);
                }

                yield return null;
            }

            view.Transform.localScale = new Vector3(SelectedCardScale, SelectedCardScale, 1f);
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
            public CardView(TarotRuntimeCard card, Transform transform, SpriteRenderer renderer, Collider2D collider, Sprite frontSprite)
            {
                Card = card;
                Transform = transform;
                Renderer = renderer;
                Collider = collider;
                FrontSprite = frontSprite;
            }

            public TarotRuntimeCard Card { get; }
            public Transform Transform { get; }
            public SpriteRenderer Renderer { get; }
            public Collider2D Collider { get; }
            public Sprite FrontSprite { get; }
            public bool IsSelected { get; set; }
        }
    }
}
