using System;
using System.Collections.Generic;
using Tarot.Readings;
using Tarot.RuntimeDeck;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tarot.DailyReading
{
    public sealed class DailyImmersiveDeckController : MonoBehaviour
    {
        private const float RotationStepDegrees = 360f / 78f;
        private const float DragSelectThreshold = 12f;
        private const float DragRotationMultiplier = 0.075f;
        private const float BaseVisibleArcDegrees = 38f;
        private const float MinimumVisibleArcDegrees = 33f;
        private const float MaximumVisibleArcDegrees = 45f;

        private readonly List<CardDrawCardView> cardViews = new();
        private Sprite cardBackSprite;
        private Color cardDimColor;
        private Color focusColor;
        private float rotationOffset;
        private bool inputEnabled = true;
        private bool isDragging;
        private bool dragExceededThreshold;
        private bool pointerDownOverUi;
        private bool layoutFrozen;
        private Vector3 mouseDownPosition;
        private Vector3 lastMousePosition;

        public event Action<CardDrawCardView> CardSelected;
        public IReadOnlyList<CardDrawCardView> CardViews => cardViews;

        private void Update()
        {
            if (cardViews.Count == 0)
            {
                return;
            }

            if (inputEnabled)
            {
                HandleInput();
            }

            if (!layoutFrozen)
            {
                UpdateCardLayout();
            }
        }

        public void Initialize(
            IReadOnlyList<TarotRuntimeCard> cards,
            Sprite backSprite,
            Func<TarotRuntimeCard, Sprite> frontSpriteProvider,
            Color dimColor,
            Color selectedFocusColor)
        {
            cardBackSprite = backSprite;
            cardDimColor = dimColor;
            focusColor = selectedFocusColor;
            ClearDeck();
            BuildDeck(cards, frontSpriteProvider);
            ResetDeck();
        }

        public void SetInputEnabled(bool isEnabled)
        {
            inputEnabled = isEnabled;
            isDragging = false;
            dragExceededThreshold = false;
        }

        public void SetLayoutFrozen(bool isFrozen)
        {
            layoutFrozen = isFrozen;
        }

        public void ResetDeck()
        {
            rotationOffset = 0f;
            layoutFrozen = false;

            foreach (var view in cardViews)
            {
                view.IsSelected = false;
                view.Transform.localRotation = Quaternion.identity;
                view.Transform.localScale = Vector3.one;
                view.Renderer.sprite = cardBackSprite;
                view.Renderer.color = cardDimColor;
                view.Renderer.sortingOrder = 1000;
            }

            SetInputEnabled(true);
            UpdateCardLayout();
        }

        private void BuildDeck(IReadOnlyList<TarotRuntimeCard> cards, Func<TarotRuntimeCard, Sprite> frontSpriteProvider)
        {
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var cardObject = new GameObject($"Daily Immersive Card {index:00} {card.EnglishName}");
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

        private void HandleInput()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Rotate(scroll * RotationStepDegrees);
            }

            if (Input.GetMouseButtonDown(0))
            {
                pointerDownOverUi = IsPointerOverUi();
                isDragging = true;
                dragExceededThreshold = false;
                mouseDownPosition = Input.mousePosition;
                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                if (!pointerDownOverUi && !dragExceededThreshold && !IsPointerOverUi())
                {
                    TrySelectClickedCard(Input.mousePosition);
                }
            }

            if (isDragging)
            {
                var delta = Input.mousePosition - lastMousePosition;
                if (Vector3.Distance(Input.mousePosition, mouseDownPosition) > DragSelectThreshold)
                {
                    dragExceededThreshold = true;
                }

                Rotate(delta.x * DragRotationMultiplier);
                lastMousePosition = Input.mousePosition;
            }
        }

        private void Rotate(float degrees)
        {
            rotationOffset += degrees;
        }

        private static bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }

        private void UpdateCardLayout()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                return;
            }

            var halfHeight = mainCamera.orthographicSize;
            var halfWidth = halfHeight * mainCamera.aspect;
            var visibleArc = GetResponsiveVisibleArcDegrees(mainCamera.aspect);
            var halfVisibleArc = visibleArc * 0.5f;
            var horizontalSpan = halfWidth * 0.82f;
            var centerY = halfHeight * 0.36f;
            var centerScale = GetResponsiveCenterScale(halfWidth, halfHeight);
            var edgeScale = centerScale * 0.66f;

            for (var index = 0; index < cardViews.Count; index++)
            {
                var angle = index * RotationStepDegrees + rotationOffset;
                var normalizedAngle = Mathf.DeltaAngle(0f, angle);
                var isVisible = Mathf.Abs(normalizedAngle) <= halfVisibleArc;
                var view = cardViews[index];
                view.Transform.gameObject.SetActive(isVisible);

                if (!isVisible || view.IsSelected)
                {
                    continue;
                }

                var sideAmount = normalizedAngle / halfVisibleArc;
                var centerProximity = 1f - Mathf.Clamp01(Mathf.Abs(sideAmount));
                var easedSide = Mathf.Sign(sideAmount) * Mathf.Pow(Mathf.Abs(sideAmount), 0.82f);
                var scale = Mathf.Lerp(edgeScale, centerScale, Smooth01(centerProximity));
                var y = centerY - Mathf.Abs(sideAmount) * halfHeight * 0.18f + centerProximity * halfHeight * 0.04f;
                var x = easedSide * horizontalSpan;
                var tint = Color.Lerp(cardDimColor, focusColor, 0.16f + centerProximity * 0.3f);
                tint.a = Mathf.Lerp(0.5f, 0.98f, centerProximity);

                view.Transform.localPosition = new Vector3(x, y, 0f);
                view.Transform.localRotation = Quaternion.identity;
                view.Transform.localScale = Vector3.one * scale;
                view.Renderer.color = tint;
                view.Renderer.sortingOrder = Mathf.RoundToInt(1000 + centerProximity * 180f);
            }
        }

        private void TrySelectClickedCard(Vector3 screenPosition)
        {
            var selected = GetClickedCard(screenPosition);
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

        private static float GetResponsiveVisibleArcDegrees(float aspect)
        {
            var aspectAdjustment = Mathf.Lerp(-5f, 5f, Mathf.InverseLerp(1.35f, 2.1f, aspect));
            return Mathf.Clamp(BaseVisibleArcDegrees + aspectAdjustment, MinimumVisibleArcDegrees, MaximumVisibleArcDegrees);
        }

        private static float GetResponsiveCenterScale(float halfWidth, float halfHeight)
        {
            var widthBased = halfWidth * 2f / 8.9f;
            var heightBased = halfHeight * 0.31f / 0.858f;
            return Mathf.Clamp(Mathf.Min(widthBased, heightBased), 1.22f, 1.58f);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
