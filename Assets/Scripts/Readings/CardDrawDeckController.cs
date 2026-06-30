using System;
using System.Collections.Generic;
using Tarot.RuntimeDeck;
using UnityEngine;

namespace Tarot.Readings
{
    public sealed class CardDrawDeckController : MonoBehaviour, ICardDrawLayoutProvider
    {
        private const int FocusIndex = 39;
        private const float RotationStepDegrees = 360f / 78f;
        private const float DragSelectThreshold = 12f;
        private const float GestureHoverRetainDuration = 0.42f;
        private const float GestureHoverPaddingPixels = 74f;
        private const float GestureHoverMaxDistancePixels = 138f;

        private readonly List<CardDrawCardView> cardViews = new();
        private readonly List<TarotRuntimeCard> sourceCards = new();
        private CardDrawLayoutProfile drawLayout = CardDrawLayoutProfile.CreateDefault();
        private Sprite cardBackSprite;
        private Color cardDimColor;
        private Color focusColor;
        private Func<TarotRuntimeCard, Sprite> frontSpriteProvider;
        private DeckLayoutMetrics layoutMetrics;
        private float rotationOffset;
        private bool isDragging;
        private bool dragExceededThreshold;
        private bool pointerDownOverUi;
        private bool inputEnabled = true;
        private CardDrawCardView gestureHoveredCard;
        private float gestureHoverRetainTimer;
        private Vector3 lastMousePosition;
        private Vector3 mouseDownPosition;

        public event Action<CardDrawCardView> CardSelected;

        public CardDrawLayoutProfile DrawLayout => GetDrawLayout();
        public IReadOnlyList<CardDrawCardView> CardViews => cardViews;

        private void Update()
        {
            if (cardViews.Count == 0)
            {
                return;
            }

            ApplyResponsiveLayout();
            UpdateGestureHoverRetention();

            if (inputEnabled)
            {
                HandleRotationInput();
            }

            UpdateCardLayout();
        }

        public void Initialize(
            CardDrawLayoutProfile layout,
            IReadOnlyList<TarotRuntimeCard> cards,
            Sprite backSprite,
            Func<TarotRuntimeCard, Sprite> frontSpriteProvider,
            Color dimColor,
            Color selectedFocusColor)
        {
            drawLayout = layout ?? CardDrawLayoutProfile.CreateDefault();
            cardBackSprite = backSprite;
            cardDimColor = dimColor;
            focusColor = selectedFocusColor;
            this.frontSpriteProvider = frontSpriteProvider;
            sourceCards.Clear();
            if (cards != null)
            {
                sourceCards.AddRange(cards);
            }

            ResetDeck();
            ApplyResponsiveLayout();
            UpdateCardLayout();
        }

        public void SetInputEnabled(bool isEnabled)
        {
            inputEnabled = isEnabled;
            isDragging = false;
            dragExceededThreshold = false;
            if (!isEnabled)
            {
                ClearGestureHover();
            }
        }

        public void SetDrawLayout(CardDrawLayoutProfile layout)
        {
            drawLayout = layout ?? CardDrawLayoutProfile.CreateDefault();
            ApplyResponsiveLayout();
            UpdateCardLayout();
        }

        public void ResetDeck()
        {
            rotationOffset = 0f;
            ClearGestureHover();
            ClearDeck();
            BuildDeck(TarotDeckShuffler.CreateShuffledCopy(sourceCards), frontSpriteProvider);

            foreach (var view in cardViews)
            {
                view.IsSelected = false;
                view.Transform.localRotation = Quaternion.identity;
                view.Transform.localScale = Vector3.one;
                view.Renderer.sprite = cardBackSprite;
                view.Renderer.color = ForceOpaque(cardDimColor);
            }

            SetInputEnabled(true);
            UpdateCardLayout();
        }

        public void RotateFromGesture(float degrees)
        {
            if (!inputEnabled || cardViews.Count == 0)
            {
                return;
            }

            Rotate(degrees);
        }

        public void UpdateGestureHover(Vector2 screenPosition)
        {
            if (!inputEnabled || cardViews.Count == 0)
            {
                ClearGestureHover();
                return;
            }

            var selected = GetGestureTargetCard(screenPosition);
            if (selected != null)
            {
                gestureHoveredCard = selected;
                gestureHoverRetainTimer = GestureHoverRetainDuration;
                return;
            }

            if (gestureHoverRetainTimer <= 0f)
            {
                gestureHoveredCard = null;
            }
        }

        public bool TrySelectGestureHoveredCard()
        {
            if (!inputEnabled || gestureHoveredCard == null || gestureHoveredCard.IsSelected)
            {
                return false;
            }

            var selected = gestureHoveredCard;
            ClearGestureHover();
            SelectCard(selected);
            return true;
        }

        public void ClearGestureHover()
        {
            gestureHoveredCard = null;
            gestureHoverRetainTimer = 0f;
        }

        private void UpdateGestureHoverRetention()
        {
            if (gestureHoverRetainTimer <= 0f)
            {
                return;
            }

            gestureHoverRetainTimer = Mathf.Max(0f, gestureHoverRetainTimer - Time.deltaTime);
        }

        private void BuildDeck(IReadOnlyList<TarotRuntimeCard> cards, Func<TarotRuntimeCard, Sprite> frontSpriteProvider)
        {
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var cardObject = new GameObject($"Draw Card {index:00} {card.EnglishName}");
                cardObject.transform.SetParent(transform, false);

                var renderer = cardObject.AddComponent<SpriteRenderer>();
                renderer.sprite = cardBackSprite;
                renderer.sortingOrder = index;

                var collider = cardObject.AddComponent<BoxCollider2D>();
                collider.size = cardBackSprite.bounds.size;

                var frontSprite = frontSpriteProvider?.Invoke(card);
                cardViews.Add(new CardDrawCardView(card, cardObject.transform, renderer, collider, frontSprite));
            }
        }

        private void ClearDeck()
        {
            foreach (var view in cardViews)
            {
                if (view.Transform != null)
                {
                    Destroy(view.Transform.gameObject);
                }
            }

            cardViews.Clear();
        }

        private void HandleRotationInput()
        {
            var scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Rotate(scroll * RotationStepDegrees);
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

                Rotate(delta.x * 0.08f);
                lastMousePosition = UnityEngine.Input.mousePosition;
            }
        }

        private void Rotate(float degrees)
        {
            rotationOffset += degrees;
        }

        private static bool IsPointerOverUi()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }

        private void UpdateCardLayout()
        {
            var layout = GetDrawLayout();
            var halfVisibleArc = Mathf.Max(0.01f, layoutMetrics.VisibleArcDegrees * 0.5f);

            for (var index = 0; index < cardViews.Count; index++)
            {
                var angle = (index - FocusIndex) * RotationStepDegrees + rotationOffset + 90f;
                var normalizedAngle = Mathf.DeltaAngle(90f, angle);
                var isVisible = Mathf.Abs(normalizedAngle) <= halfVisibleArc;
                var view = cardViews[index];

                if (view.IsSelected)
                {
                    view.Transform.gameObject.SetActive(true);
                    continue;
                }

                view.Transform.gameObject.SetActive(isVisible);

                if (!isVisible)
                {
                    continue;
                }

                var radians = angle * Mathf.Deg2Rad;
                var position = new Vector3(
                    Mathf.Cos(radians) * layoutMetrics.RingRadius,
                    Mathf.Sin(radians) * layoutMetrics.RingRadius + layoutMetrics.RingYOffset,
                    0f);
                var centerProximity = 1f - Mathf.Clamp01(Mathf.Abs(normalizedAngle) / halfVisibleArc);
                var isGestureHovered = view == gestureHoveredCard;
                var tint = Color.Lerp(cardDimColor, focusColor, 0.2f + centerProximity * 0.22f);
                if (isGestureHovered)
                {
                    tint = Color.Lerp(tint, focusColor, 0.52f);
                }

                tint.a = 1f;

                view.Transform.localPosition = position + (isGestureHovered ? Vector3.up * layout.RingCardScale * 0.16f : Vector3.zero);
                view.Transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                var scale = layout.RingCardScale * (isGestureHovered ? 1.045f : 1f);
                view.Transform.localScale = new Vector3(scale, scale, 1f);
                view.Renderer.color = tint;
                view.Renderer.sortingOrder = Mathf.RoundToInt(1000 + centerProximity * 100f + (isGestureHovered ? 36f : 0f));
            }
        }

        private static Color ForceOpaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        private void TrySelectClickedCard(Vector3 screenPosition)
        {
            var selected = GetClickedCard(screenPosition);
            if (selected == null)
            {
                return;
            }

            SelectCard(selected);
        }

        private void SelectCard(CardDrawCardView selected)
        {
            if (selected == null)
            {
                return;
            }

            selected.IsSelected = true;
            SetInputEnabled(false);
            CardSelected?.Invoke(selected);
        }

        private CardDrawCardView GetClickedCard(Vector3 screenPosition)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            var worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            var hits = Physics2D.OverlapPointAll(new Vector2(worldPosition.x, worldPosition.y));
            CardDrawCardView bestCard = null;
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

        private CardDrawCardView GetGestureTargetCard(Vector3 screenPosition)
        {
            var exactCard = GetClickedCard(screenPosition);
            if (exactCard != null)
            {
                return exactCard;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            CardDrawCardView bestCard = null;
            var bestScore = float.MinValue;
            var bestDistance = float.MaxValue;

            foreach (var view in cardViews)
            {
                if (view.IsSelected || view.Transform == null || !view.Transform.gameObject.activeSelf)
                {
                    continue;
                }

                var center = mainCamera.WorldToScreenPoint(view.Transform.position);
                var screenPoint = new Vector2(screenPosition.x, screenPosition.y);
                var distance = Vector2.Distance(screenPoint, new Vector2(center.x, center.y));
                var bounds = GetScreenBounds(mainCamera, view.Renderer.bounds);
                bounds.xMin -= GestureHoverPaddingPixels;
                bounds.xMax += GestureHoverPaddingPixels;
                bounds.yMin -= GestureHoverPaddingPixels;
                bounds.yMax += GestureHoverPaddingPixels;

                if (!bounds.Contains(screenPoint) && distance > GestureHoverMaxDistancePixels)
                {
                    continue;
                }

                var proximityScore = 1f - Mathf.Clamp01(distance / Mathf.Max(1f, GestureHoverMaxDistancePixels));
                var sortingScore = view.Renderer.sortingOrder * 0.001f;
                var score = proximityScore + sortingScore;
                if (score > bestScore || (Mathf.Approximately(score, bestScore) && distance < bestDistance))
                {
                    bestScore = score;
                    bestDistance = distance;
                    bestCard = view;
                }
            }

            return bestCard;
        }

        private static Rect GetScreenBounds(Camera camera, Bounds bounds)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    var corner = bounds.center + new Vector3(bounds.extents.x * x, bounds.extents.y * y, 0f);
                    var screen = camera.WorldToScreenPoint(corner);
                    min = Vector2.Min(min, new Vector2(screen.x, screen.y));
                    max = Vector2.Max(max, new Vector2(screen.x, screen.y));
                }
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private CardDrawLayoutProfile GetDrawLayout()
        {
            if (drawLayout == null)
            {
                drawLayout = CardDrawLayoutProfile.CreateDefault();
            }

            return drawLayout;
        }

        private void ApplyResponsiveLayout()
        {
            var layout = GetDrawLayout();
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                layoutMetrics = new DeckLayoutMetrics(34f, -30.8f, layout.VisibleArcDegrees);
                return;
            }

            var halfHeight = mainCamera.orthographicSize;
            var halfWidth = halfHeight * mainCamera.aspect;
            var ringRadius = Mathf.Max(0.01f, halfHeight * layout.RingRadiusHalfHeightMultiplier);
            var ringYOffset = halfHeight * layout.RingCenterYOffsetHalfHeightMultiplier;
            var visibleArcDegrees = GetResponsiveVisibleArcDegrees(layout, halfWidth, ringRadius);
            layoutMetrics = new DeckLayoutMetrics(ringRadius, ringYOffset, visibleArcDegrees);
        }

        private float GetResponsiveVisibleArcDegrees(CardDrawLayoutProfile layout, float halfWidth, float ringRadius)
        {
            var cardHalfWidth = cardBackSprite != null
                ? cardBackSprite.bounds.extents.x * layout.RingCardScale
                : 0.5f * layout.RingCardScale;
            var allowedHalfWidth = halfWidth + cardHalfWidth * layout.EdgeCropAllowance;
            var maxHalfArc = Mathf.Asin(Mathf.Clamp01(allowedHalfWidth / ringRadius)) * Mathf.Rad2Deg;
            return Mathf.Min(layout.VisibleArcDegrees, maxHalfArc * 2f);
        }

        private readonly struct DeckLayoutMetrics
        {
            public DeckLayoutMetrics(float ringRadius, float ringYOffset, float visibleArcDegrees)
            {
                RingRadius = ringRadius;
                RingYOffset = ringYOffset;
                VisibleArcDegrees = visibleArcDegrees;
            }

            public float RingRadius { get; }
            public float RingYOffset { get; }
            public float VisibleArcDegrees { get; }
        }
    }

    public sealed class CardDrawCardView
    {
        public CardDrawCardView(TarotRuntimeCard card, Transform transform, SpriteRenderer renderer, Collider2D collider, Sprite frontSprite)
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
