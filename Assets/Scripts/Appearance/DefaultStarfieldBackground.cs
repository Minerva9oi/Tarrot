using System.Collections.Generic;
using UnityEngine;

namespace Tarot.Appearance
{
    public sealed class DefaultStarfieldBackground : MonoBehaviour, IReadingEffectBackground
    {
        private const int StarCount = 135;
        private const int SortingOrder = -100;
        private const int PlacementAttempts = 16;
        private const float MinStarDistance = 0.42f;

        [SerializeField] private Color nearBlack = new(0.005f, 0.006f, 0.01f, 1f);
        [SerializeField] private Color starColor = new(0.82f, 0.88f, 1f, 1f);
        [SerializeField] private Color warmStarColor = new(1f, 0.9f, 0.68f, 1f);
        [SerializeField] private float idleBreathSpeed = 0.18f;
        [SerializeField] private float awakenedBrightness = 1.65f;
        [SerializeField] private float gatherSpeed = 2.8f;

        private readonly List<Star> stars = new();
        private Sprite starSprite;
        private Camera targetCamera;
        private Vector3 gatherTarget;

        public ReadingBackgroundState State { get; private set; } = ReadingBackgroundState.Idle;

        private void Awake()
        {
            targetCamera = Camera.main;
            starSprite = CreateStarSprite();
            CreateStars();
            ApplyCameraBackground();
        }

        private void Update()
        {
            var time = Time.time;

            foreach (var star in stars)
            {
                UpdateStarPosition(star, time);
                UpdateStarVisual(star, time);
            }

            if (State == ReadingBackgroundState.Restoring)
            {
                State = ReadingBackgroundState.Idle;
            }
        }

        public void SetIdle()
        {
            State = ReadingBackgroundState.Idle;
        }

        public void Awaken()
        {
            State = ReadingBackgroundState.Awakened;
        }

        public void GatherTo(Vector3 worldPosition)
        {
            gatherTarget = worldPosition;
            State = ReadingBackgroundState.Gathering;
        }

        public void Restore()
        {
            State = ReadingBackgroundState.Restoring;
        }

        private void ApplyCameraBackground()
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = nearBlack;
        }

        private void CreateStars()
        {
            stars.Clear();

            for (var index = 0; index < StarCount; index++)
            {
                var starObject = new GameObject($"Star {index:000}");
                starObject.transform.SetParent(transform, false);

                var renderer = starObject.AddComponent<SpriteRenderer>();
                renderer.sprite = starSprite;
                renderer.sortingOrder = SortingOrder;

                var starKind = RollStarKind();
                var star = new Star(
                    starObject.transform,
                    renderer,
                    PickStarPosition(),
                    Random.Range(starKind.MinSize, starKind.MaxSize),
                    Random.Range(starKind.MinAlpha, starKind.MaxAlpha),
                    Random.Range(0f, Mathf.PI * 2f),
                    Random.value > 0.88f,
                    starKind.BreathAmount);

                star.Transform.localPosition = star.HomePosition;
                stars.Add(star);
            }
        }

        private void UpdateStarPosition(Star star, float time)
        {
            if (State == ReadingBackgroundState.Gathering)
            {
                star.Transform.localPosition = Vector3.Lerp(
                    star.Transform.localPosition,
                    transform.InverseTransformPoint(gatherTarget),
                    Time.deltaTime * gatherSpeed);
                return;
            }

            var drift = new Vector3(
                Mathf.Sin(time * 0.18f + star.Phase) * 0.025f,
                Mathf.Cos(time * 0.14f + star.Phase) * 0.025f,
                0f);

            star.Transform.localPosition = Vector3.Lerp(
                star.Transform.localPosition,
                star.HomePosition + drift,
                Time.deltaTime * 1.4f);
        }

        private void UpdateStarVisual(Star star, float time)
        {
            var breath = 1f + Mathf.Sin(time * idleBreathSpeed + star.Phase) * star.BreathAmount;
            var stateBoost = State switch
            {
                ReadingBackgroundState.Awakened => awakenedBrightness,
                ReadingBackgroundState.Gathering => 2.1f,
                ReadingBackgroundState.Restoring => 1.2f,
                _ => 1f
            };

            var alpha = Mathf.Clamp01(star.BaseAlpha * breath * stateBoost);
            var color = star.IsWarm ? warmStarColor : starColor;
            color.a = alpha;
            star.Renderer.color = color;

            var scale = star.BaseSize * (State == ReadingBackgroundState.Gathering ? 1.55f : 1f);
            star.Transform.localScale = Vector3.one * scale;
        }

        private Vector3 PickStarPosition()
        {
            var candidate = Vector3.zero;

            for (var attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                candidate = new Vector3(Random.Range(-8.9f, 8.9f), Random.Range(-5f, 5f), 0f);
                if (HasEnoughSpace(candidate))
                {
                    return candidate;
                }
            }

            return candidate;
        }

        private bool HasEnoughSpace(Vector3 candidate)
        {
            foreach (var star in stars)
            {
                if (Vector3.Distance(candidate, star.HomePosition) < MinStarDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static StarKind RollStarKind()
        {
            var roll = Random.value;

            if (roll > 0.94f)
            {
                return new StarKind(0.16f, 0.24f, 0.9f, 1f, 0.1f);
            }

            if (roll > 0.72f)
            {
                return new StarKind(0.08f, 0.14f, 0.48f, 0.8f, 0.08f);
            }

            return new StarKind(0.032f, 0.075f, 0.2f, 0.48f, 0.055f);
        }

        private static Sprite CreateStarSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    var alpha = Mathf.Clamp01(1f - distance);
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private sealed class Star
        {
            public Star(Transform transform, SpriteRenderer renderer, Vector3 homePosition, float baseSize, float baseAlpha, float phase, bool isWarm, float breathAmount)
            {
                Transform = transform;
                Renderer = renderer;
                HomePosition = homePosition;
                BaseSize = baseSize;
                BaseAlpha = baseAlpha;
                Phase = phase;
                IsWarm = isWarm;
                BreathAmount = breathAmount;
            }

            public Transform Transform { get; }
            public SpriteRenderer Renderer { get; }
            public Vector3 HomePosition { get; }
            public float BaseSize { get; }
            public float BaseAlpha { get; }
            public float Phase { get; }
            public bool IsWarm { get; }
            public float BreathAmount { get; }
        }

        private readonly struct StarKind
        {
            public StarKind(float minSize, float maxSize, float minAlpha, float maxAlpha, float breathAmount)
            {
                MinSize = minSize;
                MaxSize = maxSize;
                MinAlpha = minAlpha;
                MaxAlpha = maxAlpha;
                BreathAmount = breathAmount;
            }

            public float MinSize { get; }
            public float MaxSize { get; }
            public float MinAlpha { get; }
            public float MaxAlpha { get; }
            public float BreathAmount { get; }
        }
    }
}
