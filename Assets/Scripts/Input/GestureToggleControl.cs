using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tarot.Input
{
    public sealed class GestureToggleControl : MonoBehaviour
    {
        private const float AnimationDuration = 0.18f;

        private readonly Color labelOffColor = new(0.56f, 0.58f, 0.6f, 0.9f);
        private readonly Color labelOnColor = new(0.9f, 0.72f, 0.36f, 1f);
        private readonly Color trackOffColor = new(0.03f, 0.033f, 0.04f, 0.78f);
        private readonly Color trackOnColor = new(0.58f, 0.42f, 0.12f, 0.9f);
        private readonly Color knobColor = new(0.92f, 0.9f, 0.82f, 0.96f);

        private Image trackImage;
        private Image knobImage;
        private Text labelText;
        private Text helpText;
        private RectTransform knobRect;
        private bool isOn;
        private float visualProgress;

        public event Action<bool> Toggled;
        public event Action HelpRequested;

        private void Update()
        {
            var target = isOn ? 1f : 0f;
            visualProgress = Mathf.MoveTowards(
                visualProgress,
                target,
                Time.unscaledDeltaTime / AnimationDuration);
            ApplyVisuals();
        }

        public void Initialize(Font font, Vector2 anchoredPosition)
        {
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(150f, 38f);

            labelText = CreateText(transform, font, "手势", 16, labelOffColor, TextAnchor.MiddleRight);
            labelText.rectTransform.anchorMin = new Vector2(0f, 0f);
            labelText.rectTransform.anchorMax = new Vector2(0f, 1f);
            labelText.rectTransform.pivot = new Vector2(0f, 0.5f);
            labelText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            labelText.rectTransform.sizeDelta = new Vector2(56f, 38f);

            helpText = CreateText(transform, font, "?", 15, labelOffColor, TextAnchor.MiddleCenter);
            helpText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            helpText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            helpText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            helpText.rectTransform.anchoredPosition = new Vector2(-82f, 0f);
            helpText.rectTransform.sizeDelta = new Vector2(20f, 24f);
            var helpButton = helpText.gameObject.AddComponent<Button>();
            helpButton.targetGraphic = helpText;
            helpButton.onClick.AddListener(() => HelpRequested?.Invoke());

            var trackObject = new GameObject("Gesture Toggle Track");
            trackObject.transform.SetParent(transform, false);
            trackImage = trackObject.AddComponent<Image>();
            trackImage.sprite = CreateRoundedRectSprite(Color.white, 68, 36, 18);
            trackImage.color = trackOffColor;
            var trackRect = trackImage.rectTransform;
            trackRect.anchorMin = new Vector2(1f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(1f, 0.5f);
            trackRect.anchoredPosition = new Vector2(0f, 0f);
            trackRect.sizeDelta = new Vector2(68f, 36f);

            var button = trackObject.AddComponent<Button>();
            button.targetGraphic = trackImage;
            button.onClick.AddListener(Toggle);

            var knobObject = new GameObject("Gesture Toggle Knob");
            knobObject.transform.SetParent(trackObject.transform, false);
            knobImage = knobObject.AddComponent<Image>();
            knobImage.sprite = CreateCircleSprite(36);
            knobImage.color = knobColor;
            knobImage.raycastTarget = false;
            knobRect = knobImage.rectTransform;
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(30f, 30f);

            ApplyVisuals();
        }

        public void SetState(bool value, bool instant)
        {
            isOn = value;
            if (instant)
            {
                visualProgress = isOn ? 1f : 0f;
                ApplyVisuals();
            }
        }

        private void Toggle()
        {
            isOn = !isOn;
            Toggled?.Invoke(isOn);
        }

        private void ApplyVisuals()
        {
            if (trackImage == null || knobRect == null || labelText == null || helpText == null)
            {
                return;
            }

            trackImage.color = Color.Lerp(trackOffColor, trackOnColor, visualProgress);
            labelText.color = Color.Lerp(labelOffColor, labelOnColor, visualProgress);
            helpText.color = Color.Lerp(labelOffColor, labelOnColor, visualProgress);
            knobRect.anchoredPosition = new Vector2(Mathf.Lerp(-16f, 16f, visualProgress), 0f);
        }

        private static Text CreateText(Transform parent, Font font, string value, int fontSize, Color color, TextAnchor alignment)
        {
            var textObject = new GameObject("Gesture Toggle Label");
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    var alpha = Mathf.Clamp01(1f - SmoothStep(0.82f, 1f, distance));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateRoundedRectSprite(Color color, int width, int height, int radius)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = x < radius ? radius - x : x >= width - radius ? x - (width - radius - 1) : 0;
                    var dy = y < radius ? radius - y : y >= height - radius ? y - (height - radius - 1) : 0;
                    var outsideCorner = dx > 0 || dy > 0;
                    var alpha = !outsideCorner || dx * dx + dy * dy <= radius * radius ? color.a : 0f;
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float SmoothStep(float edge0, float edge1, float value)
        {
            var t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
