using System.Collections.Generic;
using UnityEngine;

namespace Tarot.Appearance
{
    public sealed class DefaultStarfieldBackground : MonoBehaviour, IReadingEffectBackground
    {
        private const int BackgroundStarCount = 92;
        private const int SortingOrder = -100;
        private const int PlacementAttempts = 16;
        private const float MinStarDistance = 0.42f;
        private const int StarfieldSeed = 20260616;
        private const float IdleBrightnessMultiplier = 1.22f;
        private const float StarSizeMultiplier = 1.35f;

        [SerializeField] private Color nearBlack = new(0.005f, 0.006f, 0.01f, 1f);
        [SerializeField] private Color starColor = new(0.88f, 0.92f, 1f, 1f);
        [SerializeField] private Color warmStarColor = new(1f, 0.94f, 0.74f, 1f);
        [SerializeField] private float idleBreathSpeed = 0.24f;
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
            var previousRandomState = Random.state;
            Random.InitState(StarfieldSeed);
            stars.Clear();

            CreateConstellationStars();

            for (var index = 0; index < BackgroundStarCount; index++)
            {
                var starKind = RollStarKind();
                CreateStar(
                    $"Distant Star {index:000}",
                    PickStarPosition(),
                    Random.Range(starKind.MinSize, starKind.MaxSize),
                    Random.Range(starKind.MinAlpha, starKind.MaxAlpha),
                    Random.Range(0f, Mathf.PI * 2f),
                    Random.value > 0.88f,
                    starKind.BreathAmount);
            }

            Random.state = previousRandomState;
        }

        private void CreateConstellationStars()
        {
            foreach (var star in ConstellationStars)
            {
                CreateStar(
                    star.Name,
                    new Vector3(star.X, star.Y, 0f),
                    star.Size,
                    star.Alpha,
                    star.Phase,
                    star.IsWarm,
                    star.BreathAmount);
            }
        }

        private void CreateStar(string starName, Vector3 position, float baseSize, float baseAlpha, float phase, bool isWarm, float breathAmount)
        {
            var starObject = new GameObject(starName);
            starObject.transform.SetParent(transform, false);

            var renderer = starObject.AddComponent<SpriteRenderer>();
            renderer.sprite = starSprite;
            renderer.sortingOrder = SortingOrder;

            var star = new Star(
                starObject.transform,
                renderer,
                position,
                baseSize,
                baseAlpha,
                phase,
                isWarm,
                breathAmount);

            star.Transform.localPosition = star.HomePosition;
            stars.Add(star);
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

            var alpha = Mathf.Clamp01(star.BaseAlpha * breath * stateBoost * IdleBrightnessMultiplier);
            var color = star.IsWarm ? warmStarColor : starColor;
            color.a = alpha;
            star.Renderer.color = color;

            var scale = star.BaseSize * StarSizeMultiplier * (State == ReadingBackgroundState.Gathering ? 1.55f : 1f);
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
                return new StarKind(0.08f, 0.13f, 0.88f, 1f, 0.18f);
            }

            if (roll > 0.72f)
            {
                return new StarKind(0.045f, 0.085f, 0.56f, 0.86f, 0.14f);
            }

            return new StarKind(0.022f, 0.048f, 0.3f, 0.56f, 0.1f);
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
                    var core = distance < 0.14f ? 1f : 0f;
                    var halo = Mathf.Pow(Mathf.Clamp01(1f - distance), 4f) * 0.48f;
                    var alpha = Mathf.Max(core, halo);
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

        private readonly struct ConstellationStar
        {
            public ConstellationStar(string name, float x, float y, float size, float alpha, bool isWarm, float phase, float breathAmount)
            {
                Name = name;
                X = x;
                Y = y;
                Size = size;
                Alpha = alpha;
                IsWarm = isWarm;
                Phase = phase;
                BreathAmount = breathAmount;
            }

            public string Name { get; }
            public float X { get; }
            public float Y { get; }
            public float Size { get; }
            public float Alpha { get; }
            public bool IsWarm { get; }
            public float Phase { get; }
            public float BreathAmount { get; }
        }

        private static readonly ConstellationStar[] ConstellationStars =
        {
            new("Taurus Pleiades Alcyone", -6.45f, 2.78f, 0.12f, 0.96f, false, 0.1f, 0.18f),
            new("Taurus Pleiades Atlas", -6.18f, 2.94f, 0.07f, 0.66f, false, 1.9f, 0.13f),
            new("Taurus Pleiades Electra", -6.26f, 2.62f, 0.065f, 0.58f, false, 2.7f, 0.12f),
            new("Taurus Pleiades Maia", -6.02f, 2.74f, 0.06f, 0.56f, false, 3.3f, 0.12f),
            new("Taurus Pleiades Merope", -6.32f, 2.44f, 0.055f, 0.5f, false, 4.8f, 0.1f),
            new("Taurus Pleiades Taygeta", -6.58f, 2.55f, 0.048f, 0.48f, false, 5.4f, 0.1f),

            new("Taurus Aldebaran", -4.28f, 1.08f, 0.15f, 1f, true, 0.6f, 0.2f),
            new("Taurus Hyades Gamma", -4.86f, 1.54f, 0.07f, 0.64f, false, 2.2f, 0.13f),
            new("Taurus Hyades Delta", -4.72f, 0.7f, 0.064f, 0.56f, false, 3.5f, 0.12f),
            new("Taurus Hyades Epsilon", -3.72f, 1.46f, 0.062f, 0.54f, false, 4.1f, 0.12f),
            new("Taurus Hyades Theta", -3.56f, 0.62f, 0.058f, 0.5f, false, 5.6f, 0.1f),
            new("Taurus Elnath", -2.05f, 2.52f, 0.11f, 0.86f, false, 1.3f, 0.16f),
            new("Taurus Tianguan", -2.32f, 0.0f, 0.09f, 0.72f, false, 2.9f, 0.14f),

            new("Sagittarius Kaus Borealis", 4.18f, -0.48f, 0.09f, 0.78f, false, 0.8f, 0.16f),
            new("Sagittarius Kaus Media", 3.66f, -1.22f, 0.085f, 0.72f, false, 1.7f, 0.14f),
            new("Sagittarius Kaus Australis", 3.78f, -2.02f, 0.12f, 0.92f, false, 2.6f, 0.18f),
            new("Sagittarius Ascella", 4.68f, -2.1f, 0.09f, 0.76f, false, 3.4f, 0.14f),
            new("Sagittarius Alnasl", 2.88f, -1.58f, 0.075f, 0.64f, true, 4.2f, 0.12f),
            new("Sagittarius Nunki", 5.4f, -0.98f, 0.11f, 0.88f, false, 5.0f, 0.16f),
            new("Sagittarius Tau", 5.64f, -1.76f, 0.07f, 0.6f, false, 5.8f, 0.12f),
            new("Sagittarius Phi", 4.6f, -1.24f, 0.062f, 0.56f, false, 1.1f, 0.1f)
        };
    }
}
