using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tarot.Appearance
{
    public sealed class DefaultStarfieldBackground : MonoBehaviour, IReadingEffectBackground
    {
        private const int BackgroundStarCount = 320;
        private const int SortingOrder = -100;
        private const int PlacementAttempts = 20;
        private const float MinStarDistance = 0.28f;
        private const int StarfieldSeed = 20260616;
        private const float IdleBrightnessMultiplier = 1.62f;
        private const float StarSizeMultiplier = 1.88f;
        private const int MeteorSortingOrder = -80;
        private const float MeteorMinInterval = 8.5f;
        private const float MeteorMaxInterval = 15f;
        private const float RotationMeteorCooldown = 3f;
        private const float RotationMeteorDegreesPerTrail = 7.5f;
        private const float StarfieldOverscan = 1.72f;
        private const float StarTrailMotionGap = 0.18f;
        private const float StarTrailSpeedThreshold = 22f;
        private const float StarTrailFullSpeed = 92f;
        private const float StarTrailSustainThreshold = 0.58f;
        private const float StarTrailFullSustain = 1.45f;
        private const float StarTrailFadeInSpeed = 5.2f;
        private const float StarTrailFadeOutSpeed = 1.35f;
        private const int StarTrailPointCount = 12;
        private const float StarTrailMaxArcDegrees = 34f;
        private const float StarTrailOuterRadius = 11.5f;

        [SerializeField] private Color nearBlack = new(0.005f, 0.006f, 0.01f, 1f);
        [SerializeField] private Color starColor = new(0.88f, 0.92f, 1f, 1f);
        [SerializeField] private Color warmStarColor = new(1f, 0.94f, 0.74f, 1f);
        [SerializeField] private float idleBreathSpeed = 0.24f;
        [SerializeField] private float awakenedBrightness = 1.65f;
        [SerializeField] private float gatherSpeed = 2.8f;

        private readonly List<Star> stars = new();
        private Sprite starSprite;
        private Sprite meteorSprite;
        private Material starTrailMaterial;
        private Camera targetCamera;
        private Transform starRoot;
        private Vector3 gatherTarget;
        private float starfieldRotationDegrees;
        private float nextMeteorTime;
        private float nextRotationMeteorTime;
        private float rotationMeteorDegrees;
        private float rotationMotionDuration;
        private float lastRotationMotionTime;
        private float starTrailTargetIntensity;
        private float starTrailIntensity;
        private float starTrailDirection = 1f;

        public ReadingBackgroundState State { get; private set; } = ReadingBackgroundState.Idle;

        private void Awake()
        {
            targetCamera = Camera.main;
            starSprite = CreateStarSprite();
            meteorSprite = CreateMeteorSprite();
            starTrailMaterial = CreateStarTrailMaterial();
            starRoot = new GameObject("Starfield Rotation Root").transform;
            starRoot.SetParent(transform, false);
            CreateStars();
            ApplyCameraBackground();
            ScheduleNextMeteor();
        }

        private void Update()
        {
            var time = Time.time;
            UpdateStarTrailState();

            foreach (var star in stars)
            {
                UpdateStarPosition(star, time);
                UpdateStarVisual(star, time);
            }

            if (time >= nextMeteorTime)
            {
                SpawnMeteor(1f, false);
                ScheduleNextMeteor();
            }

            if (State == ReadingBackgroundState.Restoring)
            {
                State = ReadingBackgroundState.Idle;
            }
        }

        public void RotateByDeckDegrees(float degrees)
        {
            if (Mathf.Abs(degrees) < 0.001f)
            {
                return;
            }

            starfieldRotationDegrees += degrees;
            if (starRoot != null)
            {
                starRoot.localRotation = Quaternion.Euler(0f, 0f, starfieldRotationDegrees);
            }
        }

        public void TriggerRotationMeteorTrail(float degrees)
        {
            if (Mathf.Abs(degrees) < 0.01f)
            {
                return;
            }

            UpdateStarTrailSignal(degrees);
            rotationMeteorDegrees += Mathf.Abs(degrees);
            if (Time.time < nextRotationMeteorTime || rotationMeteorDegrees < RotationMeteorDegreesPerTrail)
            {
                return;
            }

            rotationMeteorDegrees = 0f;
            nextRotationMeteorTime = Time.time + RotationMeteorCooldown;
            SpawnMeteor(Mathf.Sign(degrees), true);
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
            starObject.transform.SetParent(starRoot != null ? starRoot : transform, false);

            var renderer = starObject.AddComponent<SpriteRenderer>();
            renderer.sprite = starSprite;
            renderer.sortingOrder = SortingOrder;
            var trailRenderer = CreateStarTrailRenderer(starName);

            var star = new Star(
                starObject.transform,
                renderer,
                trailRenderer,
                position,
                baseSize,
                baseAlpha,
                phase,
                isWarm,
                breathAmount);

            star.Transform.localPosition = star.HomePosition;
            stars.Add(star);
        }

        private LineRenderer CreateStarTrailRenderer(string starName)
        {
            if (starTrailMaterial == null)
            {
                return null;
            }

            var trailObject = new GameObject($"{starName} Arc Trail");
            trailObject.transform.SetParent(starRoot != null ? starRoot : transform, false);

            var trailRenderer = trailObject.AddComponent<LineRenderer>();
            trailRenderer.sharedMaterial = starTrailMaterial;
            trailRenderer.positionCount = StarTrailPointCount;
            trailRenderer.useWorldSpace = false;
            trailRenderer.loop = false;
            trailRenderer.numCapVertices = 2;
            trailRenderer.numCornerVertices = 2;
            trailRenderer.textureMode = LineTextureMode.Stretch;
            trailRenderer.sortingOrder = SortingOrder + 1;
            trailRenderer.enabled = false;
            return trailRenderer;
        }

        private void UpdateStarPosition(Star star, float time)
        {
            if (State == ReadingBackgroundState.Gathering)
            {
                star.Transform.localPosition = Vector3.Lerp(
                    star.Transform.localPosition,
                    starRoot != null ? starRoot.InverseTransformPoint(gatherTarget) : transform.InverseTransformPoint(gatherTarget),
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
            color.a = Mathf.Clamp01(alpha * Mathf.Lerp(1f, 1.62f, starTrailIntensity));
            star.Renderer.color = color;

            var scale = star.BaseSize * StarSizeMultiplier * (State == ReadingBackgroundState.Gathering ? 1.55f : 1f);
            if (starTrailIntensity > 0.01f && State != ReadingBackgroundState.Gathering)
            {
                star.Transform.localRotation = Quaternion.identity;
                star.Transform.localScale = Vector3.one * (scale * Mathf.Lerp(1f, 0.78f, starTrailIntensity));
                UpdateStarArcTrail(star, color, scale);
                return;
            }

            SetStarArcTrailVisible(star, false);
            star.Transform.localRotation = Quaternion.identity;
            star.Transform.localScale = Vector3.one * scale;
        }

        private void UpdateStarArcTrail(Star star, Color headColor, float starScale)
        {
            if (star.Trail == null)
            {
                return;
            }

            var currentPosition = star.Transform.localPosition;
            var radius = currentPosition.magnitude;
            if (radius < 0.4f)
            {
                SetStarArcTrailVisible(star, false);
                return;
            }

            var radiusFactor = Smooth01(Mathf.InverseLerp(1.2f, StarTrailOuterRadius, radius));
            var arcDegrees = Mathf.Lerp(5f, StarTrailMaxArcDegrees, radiusFactor) *
                Mathf.Lerp(0.18f, 1f, starTrailIntensity);
            var direction = starTrailDirection >= 0f ? 1f : -1f;
            var headAngle = Mathf.Atan2(currentPosition.y, currentPosition.x) * Mathf.Rad2Deg;
            var tailAngle = headAngle - direction * arcDegrees;

            for (var index = 0; index < StarTrailPointCount; index++)
            {
                var t = index / (float)(StarTrailPointCount - 1);
                var angle = Mathf.Lerp(tailAngle, headAngle, t) * Mathf.Deg2Rad;
                var radiusJitter = Mathf.Sin(t * Mathf.PI * 2f + star.Phase) * 0.012f * radiusFactor;
                star.Trail.SetPosition(index, new Vector3(
                    Mathf.Cos(angle) * (radius + radiusJitter),
                    Mathf.Sin(angle) * (radius + radiusJitter),
                    0f));
            }

            var tailColor = headColor;
            tailColor.a = headColor.a * Mathf.Lerp(0.04f, 0.14f, starTrailIntensity);
            var endColor = headColor;
            endColor.a = headColor.a * Mathf.Lerp(0.4f, 0.86f, starTrailIntensity);
            star.Trail.startColor = tailColor;
            star.Trail.endColor = endColor;
            var width = starScale * Mathf.Lerp(0.12f, 0.32f, 1f - radiusFactor);
            star.Trail.startWidth = width * 0.34f;
            star.Trail.endWidth = width;
            SetStarArcTrailVisible(star, true);
        }

        private static void SetStarArcTrailVisible(Star star, bool isVisible)
        {
            if (star.Trail != null)
            {
                star.Trail.enabled = isVisible;
            }
        }

        private void UpdateStarTrailState()
        {
            if (Time.time - lastRotationMotionTime > StarTrailMotionGap)
            {
                rotationMotionDuration = 0f;
                starTrailTargetIntensity = 0f;
            }

            var speed = starTrailTargetIntensity > starTrailIntensity ? StarTrailFadeInSpeed : StarTrailFadeOutSpeed;
            starTrailIntensity = Mathf.MoveTowards(starTrailIntensity, starTrailTargetIntensity, Time.deltaTime * speed);
        }

        private void UpdateStarTrailSignal(float degrees)
        {
            var now = Time.time;
            if (now - lastRotationMotionTime > StarTrailMotionGap)
            {
                rotationMotionDuration = 0f;
            }

            var frameDuration = Mathf.Max(Time.deltaTime, 0.016f);
            rotationMotionDuration += frameDuration;
            lastRotationMotionTime = now;
            starTrailDirection = Mathf.Sign(degrees);

            var rotationSpeed = Mathf.Abs(degrees) / frameDuration;
            var speedIntensity = Mathf.InverseLerp(StarTrailSpeedThreshold, StarTrailFullSpeed, rotationSpeed);
            var sustainIntensity = Mathf.InverseLerp(StarTrailSustainThreshold, StarTrailFullSustain, rotationMotionDuration);
            starTrailTargetIntensity = Mathf.Clamp01(Mathf.Max(
                starTrailTargetIntensity,
                Mathf.Max(speedIntensity, sustainIntensity)));
        }

        private void ScheduleNextMeteor()
        {
            nextMeteorTime = Time.time + Random.Range(MeteorMinInterval, MeteorMaxInterval);
        }

        private void SpawnMeteor(float directionSign, bool isRotationTrail)
        {
            if (meteorSprite == null)
            {
                return;
            }

            var meteorObject = new GameObject(isRotationTrail ? "Deck Rotation Meteor Trail" : "Random Meteor");
            meteorObject.transform.SetParent(transform, false);

            var renderer = meteorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = meteorSprite;
            renderer.sortingOrder = MeteorSortingOrder + Random.Range(0, 8);
            renderer.color = new Color(0.74f, 0.86f, 1f, 0f);

            var start = PickMeteorStart(isRotationTrail);
            var travel = isRotationTrail
                ? PickRotationMeteorTravel(directionSign)
                : PickRandomMeteorTravel();
            var angle = Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg;
            var duration = isRotationTrail ? Random.Range(0.58f, 0.82f) : Random.Range(0.82f, 1.18f);
            var lengthScale = isRotationTrail ? Random.Range(1.45f, 2.2f) : Random.Range(1.8f, 2.75f);
            var thicknessScale = isRotationTrail ? Random.Range(0.34f, 0.5f) : Random.Range(0.42f, 0.62f);
            var peakAlpha = isRotationTrail ? 0.56f : 0.72f;

            meteorObject.transform.localPosition = start;
            meteorObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            meteorObject.transform.localScale = new Vector3(lengthScale, thicknessScale, 1f);

            StartCoroutine(AnimateMeteor(
                meteorObject.transform,
                renderer,
                start,
                start + travel,
                duration,
                meteorObject.transform.localScale,
                peakAlpha));
        }

        private Vector3 PickMeteorStart(bool preferUpperRight)
        {
            var halfHeight = targetCamera != null && targetCamera.orthographic ? targetCamera.orthographicSize : 5.4f;
            var halfWidth = targetCamera != null && targetCamera.orthographic ? halfHeight * targetCamera.aspect : 9.6f;

            if (preferUpperRight || Random.value < 0.72f)
            {
                return new Vector3(
                    Random.Range(halfWidth * 0.16f, halfWidth * 0.98f),
                    Random.Range(halfHeight * 0.12f, halfHeight * 0.9f),
                    0f);
            }

            return new Vector3(
                Random.Range(-halfWidth * 0.82f, halfWidth * 0.82f),
                Random.Range(-halfHeight * 0.76f, halfHeight * 0.76f),
                0f);
        }

        private static Vector3 PickRotationMeteorTravel(float directionSign)
        {
            var baseDirection = new Vector2(-0.86f, -0.5f);
            var signedNudge = directionSign >= 0f ? 0.09f : -0.09f;
            var direction = (baseDirection + new Vector2(signedNudge, Random.Range(-0.1f, 0.12f))).normalized;
            return new Vector3(direction.x, direction.y, 0f) * Random.Range(2.6f, 3.8f);
        }

        private static Vector3 PickRandomMeteorTravel()
        {
            var x = Random.Range(-4.8f, -3.1f);
            var y = Random.Range(-2.2f, -0.95f);
            return new Vector3(x, y, 0f);
        }

        private static IEnumerator AnimateMeteor(
            Transform meteor,
            SpriteRenderer renderer,
            Vector3 start,
            Vector3 end,
            float duration,
            Vector3 startScale,
            float peakAlpha)
        {
            var elapsed = 0f;

            while (elapsed < duration && meteor != null && renderer != null)
            {
                elapsed += Time.deltaTime;
                var linear = Mathf.Clamp01(elapsed / duration);
                var eased = Smooth01(linear);
                meteor.localPosition = Vector3.Lerp(start, end, eased);
                meteor.localScale = new Vector3(
                    Mathf.Lerp(startScale.x * 0.72f, startScale.x * 1.08f, eased),
                    Mathf.Lerp(startScale.y * 0.88f, startScale.y * 0.46f, eased),
                    1f);

                var color = renderer.color;
                color.a = Mathf.Sin(linear * Mathf.PI) * peakAlpha;
                renderer.color = color;
                yield return null;
            }

            if (meteor != null)
            {
                Destroy(meteor.gameObject);
            }
        }

        private Vector3 PickStarPosition()
        {
            var candidate = Vector3.zero;
            var halfExtents = GetStarfieldHalfExtents();

            for (var attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                candidate = new Vector3(
                    Random.Range(-halfExtents.x, halfExtents.x),
                    Random.Range(-halfExtents.y, halfExtents.y),
                    0f);
                if (HasEnoughSpace(candidate))
                {
                    return candidate;
                }
            }

            return candidate;
        }

        private Vector2 GetStarfieldHalfExtents()
        {
            var halfHeight = targetCamera != null && targetCamera.orthographic ? targetCamera.orthographicSize : 5.4f;
            var halfWidth = targetCamera != null && targetCamera.orthographic ? halfHeight * targetCamera.aspect : 9.6f;
            return new Vector2(halfWidth * StarfieldOverscan, halfHeight * StarfieldOverscan);
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
                return new StarKind(0.11f, 0.18f, 0.9f, 1f, 0.18f);
            }

            if (roll > 0.72f)
            {
                return new StarKind(0.064f, 0.12f, 0.62f, 0.9f, 0.14f);
            }

            return new StarKind(0.034f, 0.07f, 0.36f, 0.62f, 0.1f);
        }

        private static Sprite CreateStarSprite()
        {
            const int size = 48;
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
                    var core = Mathf.Pow(Mathf.Clamp01(1f - distance * 4.2f), 1.4f);
                    var halo = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.8f) * 0.58f;
                    var alpha = Mathf.Clamp01(core + halo);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateMeteorSprite()
        {
            const int width = 160;
            const int height = 28;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var centerY = (height - 1) * 0.5f;
            var radiusY = height * 0.5f;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var normalizedX = x / (float)(width - 1);
                    var normalizedY = Mathf.Abs(y - centerY) / radiusY;
                    var tail = Mathf.Pow(Mathf.Clamp01(normalizedX), 1.85f);
                    var crossSection = Mathf.Pow(Mathf.Clamp01(1f - normalizedY), 2.2f);
                    var head = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(normalizedX - 0.88f) * 7.8f), 2.1f);
                    var alpha = Mathf.Clamp01((tail * 0.62f + head * 0.85f) * crossSection);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.82f, 0.5f), 48f);
        }

        private static Material CreateStarTrailMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private sealed class Star
        {
            public Star(
                Transform transform,
                SpriteRenderer renderer,
                LineRenderer trail,
                Vector3 homePosition,
                float baseSize,
                float baseAlpha,
                float phase,
                bool isWarm,
                float breathAmount)
            {
                Transform = transform;
                Renderer = renderer;
                Trail = trail;
                HomePosition = homePosition;
                BaseSize = baseSize;
                BaseAlpha = baseAlpha;
                Phase = phase;
                IsWarm = isWarm;
                BreathAmount = breathAmount;
            }

            public Transform Transform { get; }
            public SpriteRenderer Renderer { get; }
            public LineRenderer Trail { get; }
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
