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
        private const float BaseVisibleArcDegrees = 23f;
        private const float MinimumVisibleArcDegrees = 20f;
        private const float MaximumVisibleArcDegrees = 26f;
        private const float EdgeTrapezoidStart = 0.68f;
        private const float MaximumTrapezoidAmount = 0.13f;
        private const float MaximumTrapezoidWidthTrim = 0.07f;

        private readonly List<CardDrawCardView> cardViews = new();
        private readonly Dictionary<CardDrawCardView, DailyCardTrapezoidRenderer> trapezoidViews = new();
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
            if (isFrozen)
            {
                HideAllTrapezoids();
            }
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
                view.Renderer.enabled = true;
                view.Renderer.sprite = cardBackSprite;
                view.Renderer.color = cardDimColor;
                view.Renderer.sortingOrder = 1000;
                SetTrapezoidVisible(view, false);
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
                var view = new CardDrawCardView(card, cardObject.transform, renderer, collider, frontSprite);
                var trapezoidView = cardObject.AddComponent<DailyCardTrapezoidRenderer>();
                trapezoidView.Initialize(renderer);
                cardViews.Add(view);
                trapezoidViews.Add(view, trapezoidView);
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
            trapezoidViews.Clear();
        }

        private void HandleInput()
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

                Rotate(delta.x * DragRotationMultiplier);
                lastMousePosition = UnityEngine.Input.mousePosition;
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
            var ringRadius = GetResponsiveRingRadius(halfWidth);
            var centerY = halfHeight * 0.04f;
            var centerScale = GetResponsiveCenterScale(halfWidth, halfHeight);
            var edgeScale = centerScale * 1.08f;

            for (var index = 0; index < cardViews.Count; index++)
            {
                var angle = index * RotationStepDegrees + rotationOffset;
                var normalizedAngle = Mathf.DeltaAngle(0f, angle);
                var isVisible = Mathf.Abs(normalizedAngle) <= halfVisibleArc;
                var view = cardViews[index];
                view.Transform.gameObject.SetActive(isVisible);

                if (!isVisible || view.IsSelected)
                {
                    SetTrapezoidVisible(view, false);
                    continue;
                }

                var sideAmount = normalizedAngle / halfVisibleArc;
                var centerProximity = 1f - Mathf.Clamp01(Mathf.Abs(sideAmount));
                var sideProximity = 1f - centerProximity;
                var ringAngle = normalizedAngle * Mathf.Deg2Rad;
                var scale = Mathf.Lerp(centerScale, edgeScale, Smooth01(sideProximity));
                var x = Mathf.Sin(ringAngle) * ringRadius;
                var y = centerY - (1f - Mathf.Cos(ringAngle)) * ringRadius * 0.08f;
                var tint = Color.Lerp(cardDimColor, focusColor, 0.16f + centerProximity * 0.3f);
                tint.a = Mathf.Lerp(0.72f, 0.98f, centerProximity);

                view.Transform.localPosition = new Vector3(x, y, 0f);
                view.Transform.localRotation = Quaternion.identity;
                view.Transform.localScale = Vector3.one * scale;
                view.Renderer.color = tint;
                view.Renderer.sortingOrder = Mathf.RoundToInt(1000 + centerProximity * 180f);
                UpdateTrapezoid(view, sideAmount, sideProximity);
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
            var aspectAdjustment = Mathf.Lerp(-2f, 2f, Mathf.InverseLerp(1.35f, 2.1f, aspect));
            return Mathf.Clamp(BaseVisibleArcDegrees + aspectAdjustment, MinimumVisibleArcDegrees, MaximumVisibleArcDegrees);
        }

        private static float GetResponsiveRingRadius(float halfWidth)
        {
            return halfWidth * 4.78f;
        }

        private static float GetResponsiveCenterScale(float halfWidth, float halfHeight)
        {
            var widthBased = halfWidth * 2f / 8.1f;
            var heightBased = halfHeight * 0.38f / 0.858f;
            return Mathf.Clamp(Mathf.Min(widthBased, heightBased), 1.5f, 1.86f);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void UpdateTrapezoid(CardDrawCardView view, float sideAmount, float sideProximity)
        {
            if (!trapezoidViews.TryGetValue(view, out var trapezoidView))
            {
                return;
            }

            var trapezoidProgress = Smooth01(Mathf.InverseLerp(EdgeTrapezoidStart, 1f, sideProximity));
            if (trapezoidProgress <= 0.01f)
            {
                trapezoidView.SetVisible(false);
                return;
            }

            var sideSign = Mathf.Sign(sideAmount);
            var amount = trapezoidProgress * MaximumTrapezoidAmount;
            var widthTrim = trapezoidProgress * MaximumTrapezoidWidthTrim;
            trapezoidView.Render(view.Renderer.sprite, view.Renderer.color, view.Renderer.sortingOrder, sideSign, amount, widthTrim);
        }

        private void SetTrapezoidVisible(CardDrawCardView view, bool isVisible)
        {
            if (trapezoidViews.TryGetValue(view, out var trapezoidView))
            {
                trapezoidView.SetVisible(isVisible);
            }
        }

        private void HideAllTrapezoids()
        {
            foreach (var view in cardViews)
            {
                SetTrapezoidVisible(view, false);
            }
        }
    }

    internal sealed class DailyCardTrapezoidRenderer : MonoBehaviour
    {
        private const string SpriteShaderName = "Sprites/Default";

        private SpriteRenderer sourceRenderer;
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private Material material;

        public void Initialize(SpriteRenderer source)
        {
            sourceRenderer = source;

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            mesh = new Mesh { name = "Daily Card Trapezoid Mesh" };
            meshFilter.sharedMesh = mesh;

            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find(SpriteShaderName));
            meshRenderer.sharedMaterial = material;
            SetVisible(false);
        }

        public void SetVisible(bool isVisible)
        {
            if (sourceRenderer != null)
            {
                sourceRenderer.enabled = !isVisible;
            }

            if (meshRenderer != null)
            {
                meshRenderer.enabled = isVisible;
            }
        }

        public void Render(Sprite sprite, Color color, int sortingOrder, float sideSign, float amount, float widthTrim)
        {
            if (sprite == null || sourceRenderer == null || meshRenderer == null || material == null)
            {
                SetVisible(false);
                return;
            }

            material.mainTexture = sprite.texture;
            material.color = color;
            meshRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            meshRenderer.sortingOrder = sortingOrder;

            var bounds = sprite.bounds;
            var halfWidth = bounds.extents.x;
            var halfHeight = bounds.extents.y;
            var innerHeight = halfHeight * Mathf.Clamp01(1f - amount);
            var innerX = halfWidth * Mathf.Clamp01(1f - widthTrim);

            Vector3 bottomLeft;
            Vector3 topLeft;
            Vector3 bottomRight;
            Vector3 topRight;

            if (sideSign < 0f)
            {
                bottomLeft = new Vector3(-halfWidth, -halfHeight, 0f);
                topLeft = new Vector3(-halfWidth, halfHeight, 0f);
                bottomRight = new Vector3(innerX, -innerHeight, 0f);
                topRight = new Vector3(innerX, innerHeight, 0f);
            }
            else
            {
                bottomLeft = new Vector3(-innerX, -innerHeight, 0f);
                topLeft = new Vector3(-innerX, innerHeight, 0f);
                bottomRight = new Vector3(halfWidth, -halfHeight, 0f);
                topRight = new Vector3(halfWidth, halfHeight, 0f);
            }

            mesh.Clear();
            mesh.vertices = new[] { bottomLeft, topLeft, topRight, bottomRight };
            mesh.uv = GetSpriteUvs(sprite);
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            SetVisible(true);
        }

        private static Vector2[] GetSpriteUvs(Sprite sprite)
        {
            var texture = sprite.texture;
            var rect = sprite.textureRect;
            var xMin = rect.xMin / texture.width;
            var xMax = rect.xMax / texture.width;
            var yMin = rect.yMin / texture.height;
            var yMax = rect.yMax / texture.height;
            return new[]
            {
                new Vector2(xMin, yMin),
                new Vector2(xMin, yMax),
                new Vector2(xMax, yMax),
                new Vector2(xMax, yMin)
            };
        }
    }
}
