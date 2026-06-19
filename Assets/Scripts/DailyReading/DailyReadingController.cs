using System;
using System.Collections;
using System.Collections.Generic;
using Tarot.Appearance;
using Tarot.Cards;
using Tarot.Localization;
using Tarot.Readings;
using Tarot.RuntimeDeck;
using UnityEngine;
using UnityEngine.UI;

namespace Tarot.DailyReading
{
    public sealed class DailyReadingController : MonoBehaviour
    {
        private const int ResultFontSize = 24;
        private const int ResultBodyFontSize = 18;
        private const int ResultOrientationFontSize = 16;
        private const int WindDustCount = 520;
        private const int ImpactDustCount = 260;
        private const int ResidualGrainCount = 3600;
        private const int MeshDustParticleCount = 10000;
        private const int MeshImpactParticleCount = 2600;
        private const int MeshParticleQuadVertexCount = 4;
        private const int MeshParticleBuildChunkSize = 2800;
        private const float ResultCardScale = 2.16f;
        private const float SelectedPullScaleMultiplier = 1.055f;
        private static readonly Vector2 SelectedCardViewportPosition = new(0.5f, 0.56f);

        [SerializeField] private BackgroundManager backgroundManager;
        [SerializeField] private LocaleId currentLocale = LocaleId.SimplifiedChinese;
        [SerializeField] private Color cardBackColor = Color.white;
        [SerializeField] private Color cardFaceColor = new(0.86f, 0.84f, 0.78f, 1f);
        [SerializeField] private Color cardLineColor = new(0.78f, 0.68f, 0.48f, 1f);
        [SerializeField] private Color cardDimColor = new(0.42f, 0.44f, 0.52f, 0.88f);
        [SerializeField] private Color focusColor = new(1f, 0.92f, 0.68f, 1f);

        private Font defaultFont;
        private Transform selectedAnchor;
        private Transform effectRoot;
        private DailyImmersiveDeckController deckController;
        private Canvas canvas;
        private Text resultText;
        private bool isResolving;
        private CardDeckArtData cardDeckArt;
        private Sprite cardBackSprite;
        private Sprite fallbackCardFaceSprite;
        private Sprite starParticleSprite;
        private Material cardDustMeshMaterial;
        private CardBackParticleTemplate[] cardBackParticleTemplates;

        public event Action BackRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Microsoft YaHei", "Helvetica Neue", "Arial" },
                18);

            cardDeckArt = CardDeckArtData.LoadDefault();
            cardBackSprite = CreateCardSprite(cardBackColor, cardLineColor, true);
            fallbackCardFaceSprite = CreateCardSprite(cardFaceColor, new Color(0.28f, 0.24f, 0.2f, 1f), false);
            starParticleSprite = CreateStarParticleSprite();
            cardDustMeshMaterial = CreateCardDustMeshMaterial(starParticleSprite);
            GetCardBackParticleTemplates(cardBackSprite.bounds);

            BuildScene();
            SetResultVisible(false);
        }

        private void Update()
        {
            ApplyResponsiveLayout();
        }

        public void SetBackgroundManager(BackgroundManager manager)
        {
            backgroundManager = manager;
        }

        public void SetLocale(LocaleId locale)
        {
            currentLocale = locale;
        }

        private void BuildScene()
        {
            selectedAnchor = new GameObject("Selected Card Anchor").transform;
            selectedAnchor.SetParent(transform, false);
            selectedAnchor.localPosition = Vector3.zero;
            ApplyResponsiveLayout();

            effectRoot = new GameObject("Daily Star Reveal Effects").transform;
            effectRoot.SetParent(transform, false);

            BuildDeckController();

            canvas = CreateCanvas();
            resultText = CreateText(canvas.transform, string.Empty, ResultFontSize, new Color(0.88f, 0.86f, 0.8f, 1f), new Vector2(0f, -204f), new Vector2(920f, 154f));
            resultText.lineSpacing = 1.04f;
            resultText.supportRichText = true;

            CreateButton(canvas.transform, "返回", new Vector2(-790f, -456f), () => BackRequested?.Invoke());
            CreateButton(canvas.transform, "再抽一次", new Vector2(790f, -456f), ResetReading);
        }

        private void BuildDeckController()
        {
            var deckObject = new GameObject("Daily Immersive Draw Deck");
            deckObject.transform.SetParent(transform, false);
            deckController = deckObject.AddComponent<DailyImmersiveDeckController>();
            deckController.CardSelected += HandleCardSelected;
            deckController.DeckRotated += HandleDeckRotated;
            deckController.Initialize(
                TarotRuntimeDeck.Cards,
                cardBackSprite,
                card => cardDeckArt != null ? cardDeckArt.GetFrontSprite(card.CardId) : null,
                cardDimColor,
                focusColor);
        }

        private void HandleDeckRotated(float degrees)
        {
            backgroundManager?.RotateStarfield(degrees);
            backgroundManager?.TriggerRotationMeteorTrail(degrees);
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

        private void HandleCardSelected(CardDrawCardView selected)
        {
            if (isResolving)
            {
                return;
            }

            isResolving = true;
            deckController?.SetLayoutFrozen(true);
            backgroundManager?.Awaken();
            StartCoroutine(RevealCard(selected));
        }

        private IEnumerator RevealCard(CardDrawCardView view)
        {
            var targetPosition = selectedAnchor.localPosition;
            var orientation = UnityEngine.Random.value > 0.5f ? TarotOrientation.Upright : TarotOrientation.Reversed;

            yield return RevealCardWithWindDissolve(view, targetPosition, orientation);
            ShowResult(view.Card, orientation);
            backgroundManager?.Restore();
        }

        private void ShowResult(TarotRuntimeCard card, TarotOrientation orientation)
        {
            SetResultVisible(true);
            var cardName = FormatResultCardName(GetLocalizedCardName(card));
            var orientationText = GetLocalizedOrientation(orientation);
            resultText.text =
                $"<b>{cardName}</b>  <size={ResultOrientationFontSize}>{orientationText}</size>\n" +
                $"<size={ResultBodyFontSize}>{GetLocalizedKeywordsLabel()}：{GetDailyKeywords(card, orientation)}</size>\n" +
                $"<size={ResultBodyFontSize}>{GetLocalizedReminderLabel()}：{GetDailyReminder(card, orientation)}</size>";
        }

        private void ResetReading()
        {
            ClearEffectParticles();
            deckController?.ResetDeck();
            resultText.text = string.Empty;
            SetResultVisible(false);
            isResolving = false;
            backgroundManager?.SetIdle();
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

        private void ApplyResponsiveLayout()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                return;
            }

            if (selectedAnchor != null)
            {
                selectedAnchor.localPosition = ViewportToLocalWorldPoint(mainCamera, SelectedCardViewportPosition);
            }
        }

        private Vector3 ViewportToLocalWorldPoint(Camera mainCamera, Vector2 viewportPosition)
        {
            var depth = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            var worldPosition = mainCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, depth));
            worldPosition.z = 0f;
            return transform.InverseTransformPoint(worldPosition);
        }

        private IEnumerator RevealCardWithWindDissolve(CardDrawCardView selected, Vector3 targetPosition, TarotOrientation orientation)
        {
            const float dissolveDuration = 3.92f;
            const float particleSurfaceBuildDuration = 0.24f;
            var elapsed = 0f;
            var dissolvingCards = new List<CardDrawCardView>();
            var startColors = new Dictionary<CardDrawCardView, Color>();
            var startScales = new Dictionary<CardDrawCardView, Vector3>();
            var cardDissolveDelays = new Dictionary<CardDrawCardView, float>();
            var particleBatches = new List<CardBackParticleBatch>();
            var selectedStart = transform.InverseTransformPoint(selected.Transform.position);
            var selectedMoveStart = new Vector3(selectedStart.x, targetPosition.y, selectedStart.z);
            var selectedStartScale = selected.Transform.localScale.x;
            var selectedPullScale = ResultCardScale * SelectedPullScaleMultiplier;

            PrepareSelectedResultCard(selected, selectedMoveStart);

            foreach (var view in deckController.CardViews)
            {
                if (!view.Transform.gameObject.activeSelf)
                {
                    continue;
                }

                if (view == selected)
                {
                    continue;
                }

                dissolvingCards.Add(view);
                startColors[view] = view.Renderer.color;
                startScales[view] = view.Transform.localScale;
                cardDissolveDelays[view] = GetCardDissolveDelay();
                yield return SpawnCardBackMeshParticles(view, particleBatches, cardDissolveDelays[view]);
            }

            while (elapsed < particleSurfaceBuildDuration)
            {
                elapsed += Time.deltaTime;
                UpdateCardBackParticleBatches(particleBatches, elapsed);
                yield return null;
            }

            foreach (var view in dissolvingCards)
            {
                var hiddenColor = view.Renderer.color;
                hiddenColor.a = 0f;
                view.Renderer.color = hiddenColor;
            }

            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                var settleProgress = Smooth01(Mathf.InverseLerp(0f, 0.44f, elapsed));
                var pullProgress = Mathf.Clamp01(elapsed / 0.44f);
                var pullPulse = Mathf.Sin(pullProgress * Mathf.PI) * (selectedPullScale - ResultCardScale);
                selected.Transform.localPosition = selectedMoveStart;
                selected.Transform.localRotation = Quaternion.identity;
                selected.Transform.localScale = Vector3.one * (Mathf.Lerp(selectedStartScale, ResultCardScale, settleProgress) + pullPulse);

                foreach (var view in dissolvingCards)
                {
                    var cardFadeProgress = Smooth01(Mathf.InverseLerp(
                        cardDissolveDelays[view] + 0.72f,
                        cardDissolveDelays[view] + 2.95f,
                        elapsed));
                    var color = startColors[view];
                    color.a = 0f;
                    view.Renderer.color = color;
                    view.Transform.localScale = Vector3.Lerp(startScales[view], startScales[view] * 0.96f, cardFadeProgress);
                }

                UpdateCardBackParticleBatches(particleBatches, elapsed);

                yield return null;
            }

            foreach (var view in dissolvingCards)
            {
                view.Transform.gameObject.SetActive(false);
            }

            DestroyCardBackParticleBatches(particleBatches);
            yield return MoveSelectedCardToResult(selected, selectedMoveStart, targetPosition, orientation);
        }

        private IEnumerator MoveSelectedCardToResult(CardDrawCardView selected, Vector3 selectedStart, Vector3 targetPosition, TarotOrientation orientation)
        {
            const float moveDuration = 0.92f;
            var elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / moveDuration);
                var moveProgress = 1f - Mathf.Pow(1f - progress, 3f);

                selected.Transform.localPosition = Vector3.Lerp(selectedStart, targetPosition, moveProgress);
                selected.Transform.localRotation = Quaternion.identity;
                selected.Transform.localScale = Vector3.one * ResultCardScale;

                yield return null;
            }

            selected.Transform.localPosition = targetPosition;
            selected.Transform.localRotation = Quaternion.identity;
            selected.Transform.localScale = Vector3.one * ResultCardScale;
            yield return FlipSelectedCardAtResult(selected, orientation);
        }

        private IEnumerator FlipSelectedCardAtResult(CardDrawCardView selected, TarotOrientation orientation)
        {
            const float flipDuration = 0.62f;
            var elapsed = 0f;
            var selectedFrontApplied = false;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / flipDuration);
                var flipWidth = progress < 0.5f
                    ? Mathf.Lerp(1f, 0.08f, Smooth01(progress * 2f))
                    : Mathf.Lerp(0.08f, 1f, Smooth01((progress - 0.5f) * 2f));

                if (!selectedFrontApplied && progress >= 0.5f)
                {
                    selected.Renderer.sprite = selected.FrontSprite != null ? selected.FrontSprite : fallbackCardFaceSprite;
                    selected.Renderer.color = orientation == TarotOrientation.Upright
                        ? Color.white
                        : new Color(0.92f, 0.9f, 0.96f, 1f);
                    selected.Transform.localRotation = orientation == TarotOrientation.Upright
                        ? Quaternion.identity
                        : Quaternion.Euler(0f, 0f, 180f);
                    selectedFrontApplied = true;
                }

                selected.Transform.localScale = new Vector3(ResultCardScale * flipWidth, ResultCardScale, ResultCardScale);
                yield return null;
            }

            var finalColor = orientation == TarotOrientation.Upright
                ? Color.white
                : new Color(0.92f, 0.9f, 0.96f, 1f);
            finalColor.a = 1f;
            selected.Renderer.sprite = selected.FrontSprite != null ? selected.FrontSprite : fallbackCardFaceSprite;
            selected.Renderer.color = finalColor;
            selected.Transform.localRotation = orientation == TarotOrientation.Upright
                ? Quaternion.identity
                : Quaternion.Euler(0f, 0f, 180f);
            selected.Transform.localScale = Vector3.one * ResultCardScale;
        }

        private void PrepareSelectedResultCard(CardDrawCardView selected, Vector3 startPosition)
        {
            selected.Transform.gameObject.SetActive(true);
            selected.Transform.localPosition = startPosition;
            selected.Transform.localRotation = Quaternion.identity;
            selected.Renderer.sprite = cardBackSprite;
            selected.Renderer.sortingOrder = 2600;
            selected.Renderer.color = focusColor;
        }

        private float SpawnWindDustFromCard(CardDrawCardView view)
        {
            var start = transform.InverseTransformPoint(view.Transform.position);
            var cardBounds = cardBackSprite.bounds;
            var cardScale = view.Transform.localScale.x;
            var sweepDelay = Mathf.InverseLerp(-6.5f, 6.5f, start.x) * 0.24f;

            for (var index = 0; index < WindDustCount; index++)
            {
                var unscaledOffset = RandomCardBackOffset(cardBounds, out var normalizedX, out var normalizedY);
                var offset = new Vector3(
                    unscaledOffset.x * cardScale,
                    unscaledOffset.y * cardScale,
                    0f);
                var dustColor = SampleCardBackDustColor(unscaledOffset, cardBounds);
                var peelCoordinate = GetPeelCoordinate(normalizedX, normalizedY);
                var raggedEdge =
                    Mathf.Sin((normalizedY * 10.4f + normalizedX * 4.9f + UnityEngine.Random.value * 2.2f) * Mathf.PI) * 0.16f +
                    Mathf.Sin((normalizedY * 27.5f + UnityEngine.Random.value) * Mathf.PI) * 0.045f;
                var peelDelay = Smooth01(peelCoordinate) * 1.58f + raggedEdge + UnityEngine.Random.Range(0f, 0.22f);
                SpawnWindDust(start + offset, sweepDelay + Mathf.Max(0f, peelDelay), UnityEngine.Random.Range(2.18f, 3.15f), dustColor, false);
            }

            for (var index = 0; index < ImpactDustCount; index++)
            {
                var unscaledOffset = RandomCardBackOffset(cardBounds, out var normalizedX, out var normalizedY);
                var offset = new Vector3(
                    unscaledOffset.x * cardScale,
                    unscaledOffset.y * cardScale,
                    0f);
                var dustColor = SampleCardBackDustColor(unscaledOffset, cardBounds);
                var peelCoordinate = GetPeelCoordinate(normalizedX, normalizedY);
                var frontBreakup = Mathf.Sin((normalizedY * 18.3f + normalizedX * 31.7f + UnityEngine.Random.value * 2.5f) * Mathf.PI) * 0.1f;
                var peelDelay = Smooth01(peelCoordinate) * 1.5f + frontBreakup + UnityEngine.Random.Range(0f, 0.16f);
                SpawnWindDust(start + offset, sweepDelay + Mathf.Max(0f, peelDelay), UnityEngine.Random.Range(1.58f, 2.35f), dustColor, true);
            }

            return sweepDelay;
        }

        private static float GetCardDissolveDelay()
        {
            return UnityEngine.Random.Range(0f, 0.08f);
        }

        private IEnumerator SpawnCardBackMeshParticles(CardDrawCardView view, List<CardBackParticleBatch> batches, float baseDelay)
        {
            if (effectRoot == null || starParticleSprite == null || cardDustMeshMaterial == null)
            {
                yield break;
            }

            var start = transform.InverseTransformPoint(view.Transform.position);
            var cardBounds = cardBackSprite.bounds;
            var cardScale = view.Transform.localScale.x;
            var particleCount = MeshDustParticleCount + MeshImpactParticleCount;
            var templates = GetCardBackParticleTemplates(cardBounds);
            var particles = new CardBackMeshParticle[particleCount];
            var vertices = new Vector3[particleCount * MeshParticleQuadVertexCount];
            var colors = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[particleCount * 6];

            for (var index = 0; index < particleCount; index++)
            {
                particles[index] = CreateCardBackMeshParticle(start, cardScale, baseDelay, templates[index]);
                WriteParticleQuadStaticData(index, uvs, triangles);
                WriteParticleQuad(vertices, colors, index, particles[index].StartPosition, particles[index].StartScale, Color.clear);

                if (index > 0 && index % MeshParticleBuildChunkSize == 0)
                {
                    yield return null;
                }
            }

            var batchObject = new GameObject("Daily Card Back Mesh Dust");
            batchObject.transform.SetParent(effectRoot, false);

            var mesh = new Mesh { name = "Daily Card Back Mesh Dust" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(42f, 24f, 2f));

            var meshFilter = batchObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = batchObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = cardDustMeshMaterial;
            meshRenderer.sortingOrder = 2400;

            batches.Add(new CardBackParticleBatch(batchObject.transform, mesh, particles, vertices, colors));
        }

        private CardBackParticleTemplate[] GetCardBackParticleTemplates(Bounds cardBounds)
        {
            var particleCount = MeshDustParticleCount + MeshImpactParticleCount;
            if (cardBackParticleTemplates != null && cardBackParticleTemplates.Length == particleCount)
            {
                return cardBackParticleTemplates;
            }

            cardBackParticleTemplates = new CardBackParticleTemplate[particleCount];
            for (var index = 0; index < particleCount; index++)
            {
                var isImpact = index >= MeshDustParticleCount;
                cardBackParticleTemplates[index] = CreateCardBackParticleTemplate(cardBounds, isImpact);
            }

            return cardBackParticleTemplates;
        }

        private CardBackParticleTemplate CreateCardBackParticleTemplate(Bounds cardBounds, bool isImpact)
        {
            var offset = RandomCardBackOffset(cardBounds, out var normalizedX, out var normalizedY);
            var color = SampleCardBackExactColor(offset, cardBounds);

            var centerDistance = Vector2.Distance(new Vector2(normalizedX, normalizedY), new Vector2(0.5f, 0.5f));
            var patternHold = isImpact ? 0.46f : 0.54f;
            var releaseOffset = patternHold +
                Mathf.Clamp01(centerDistance * 1.12f) * UnityEngine.Random.Range(0.08f, 0.2f) +
                UnityEngine.Random.Range(0f, isImpact ? 0.14f : 0.24f);
            var scale = isImpact
                ? UnityEngine.Random.Range(0.18f, 0.38f)
                : UnityEngine.Random.Range(0.105f, 0.24f);
            var localDirection = new Vector2(offset.x, offset.y);
            if (localDirection.sqrMagnitude < 0.0001f)
            {
                localDirection = UnityEngine.Random.insideUnitCircle.normalized;
            }

            localDirection.Normalize();
            var outward = isImpact
                ? UnityEngine.Random.Range(0.36f, 0.98f)
                : UnityEngine.Random.Range(0.18f, 0.64f);
            var upward = isImpact
                ? UnityEngine.Random.Range(0.78f, 1.58f)
                : UnityEngine.Random.Range(0.52f, 1.16f);
            var drift = new Vector3(
                localDirection.x * outward + UnityEngine.Random.Range(-0.1f, 0.1f),
                localDirection.y * outward * 0.36f + upward,
                0f);
            var turbulence = isImpact
                ? new Vector3(UnityEngine.Random.Range(-0.28f, 0.3f), UnityEngine.Random.Range(-0.18f, 0.24f), 0f)
                : new Vector3(UnityEngine.Random.Range(-0.18f, 0.2f), UnityEngine.Random.Range(-0.14f, 0.18f), 0f);

            return new CardBackParticleTemplate(
                offset,
                scale,
                color,
                Mathf.Max(0f, releaseOffset),
                UnityEngine.Random.Range(isImpact ? 1.72f : 1.9f, isImpact ? 2.56f : 2.9f),
                drift,
                turbulence,
                UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                isImpact);
        }

        private static CardBackMeshParticle CreateCardBackMeshParticle(
            Vector3 cardStart,
            float cardScale,
            float baseDelay,
            CardBackParticleTemplate template)
        {
            return new CardBackMeshParticle(
                cardStart + new Vector3(template.Offset.x * cardScale, template.Offset.y * cardScale, 0f),
                template.StartScale,
                template.BaseColor,
                baseDelay + template.ReleaseOffset,
                template.Duration,
                template.Drift * Mathf.Lerp(0.92f, 1.12f, cardScale / ResultCardScale),
                template.Turbulence,
                template.Phase,
                template.IsImpact);
        }

        private static void UpdateCardBackParticleBatches(List<CardBackParticleBatch> batches, float elapsed)
        {
            foreach (var batch in batches)
            {
                if (batch.Mesh == null)
                {
                    continue;
                }

                for (var index = 0; index < batch.Particles.Length; index++)
                {
                    var particle = batch.Particles[index];
                    var materialize = Smooth01(Mathf.InverseLerp(
                        particle.ReleaseDelay - (particle.IsImpact ? 0.5f : 0.58f),
                        particle.ReleaseDelay - (particle.IsImpact ? 0.28f : 0.34f),
                        elapsed));
                    var loosen = Smooth01(Mathf.InverseLerp(particle.ReleaseDelay - 0.06f, particle.ReleaseDelay + 0.2f, elapsed));
                    var releaseLinear = Mathf.Clamp01(Mathf.InverseLerp(particle.ReleaseDelay, particle.ReleaseDelay + particle.Duration, elapsed));
                    var release = Smooth01(releaseLinear);
                    var tremble = new Vector3(
                        Mathf.Sin(elapsed * (particle.IsImpact ? 17.5f : 14.5f) + particle.Phase) * 0.018f,
                        Mathf.Cos(elapsed * (particle.IsImpact ? 14.8f : 11.8f) + particle.Phase) * 0.014f,
                        0f) * loosen;
                    var gust = new Vector3(
                        Mathf.Sin(releaseLinear * (particle.IsImpact ? 17f : 14f) + particle.Phase) * particle.Turbulence.x,
                        Mathf.Cos(releaseLinear * (particle.IsImpact ? 14f : 11f) + particle.Phase) * particle.Turbulence.y,
                        0f);
                    var driftEase = Smooth01(Mathf.InverseLerp(0.06f, 1f, releaseLinear));
                    var position = particle.StartPosition + tremble + (particle.Wind * driftEase) + (gust * release * 0.5f);
                    var expansionPulse = 1f + Mathf.Sin(Mathf.Clamp01(releaseLinear / 0.34f) * Mathf.PI) * (particle.IsImpact ? 0.16f : 0.1f);
                    var scale = particle.StartScale * expansionPulse * Mathf.Lerp(1f, particle.IsImpact ? 0.48f : 0.36f, release);
                    var color = particle.BaseColor;
                    var paleProgress = Smooth01(Mathf.InverseLerp(0.34f, 0.9f, release));
                    color = Color.Lerp(color, Color.Lerp(color, Color.white, particle.IsImpact ? 0.52f : 0.38f), paleProgress);
                    var fadeOutStart = particle.IsImpact ? 0.68f : 0.74f;
                    color.a = particle.BaseColor.a * materialize * (1f - Smooth01(Mathf.InverseLerp(fadeOutStart, 1f, release)));
                    WriteParticleQuad(batch.Vertices, batch.Colors, index, position, scale, color);
                }

                batch.Mesh.vertices = batch.Vertices;
                batch.Mesh.colors = batch.Colors;
            }
        }

        private static void DestroyCardBackParticleBatches(List<CardBackParticleBatch> batches)
        {
            foreach (var batch in batches)
            {
                if (batch.Mesh != null)
                {
                    Destroy(batch.Mesh);
                }

                if (batch.Transform != null)
                {
                    Destroy(batch.Transform.gameObject);
                }
            }
        }

        private static void WriteParticleQuad(Vector3[] vertices, Color[] colors, int particleIndex, Vector3 center, float scale, Color color)
        {
            var vertexIndex = particleIndex * MeshParticleQuadVertexCount;
            var half = scale * 0.5f;
            vertices[vertexIndex] = center + new Vector3(-half, -half, 0f);
            vertices[vertexIndex + 1] = center + new Vector3(-half, half, 0f);
            vertices[vertexIndex + 2] = center + new Vector3(half, half, 0f);
            vertices[vertexIndex + 3] = center + new Vector3(half, -half, 0f);

            colors[vertexIndex] = color;
            colors[vertexIndex + 1] = color;
            colors[vertexIndex + 2] = color;
            colors[vertexIndex + 3] = color;
        }

        private static void WriteParticleQuadStaticData(int particleIndex, Vector2[] uvs, int[] triangles)
        {
            var vertexIndex = particleIndex * MeshParticleQuadVertexCount;
            uvs[vertexIndex] = new Vector2(0f, 0f);
            uvs[vertexIndex + 1] = new Vector2(0f, 1f);
            uvs[vertexIndex + 2] = new Vector2(1f, 1f);
            uvs[vertexIndex + 3] = new Vector2(1f, 0f);

            var triangleIndex = particleIndex * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        private void SpawnCardBackResidualGrains(CardDrawCardView view, List<CardBackResidualGrain> grains, float baseDelay)
        {
            if (effectRoot == null || starParticleSprite == null)
            {
                return;
            }

            var start = transform.InverseTransformPoint(view.Transform.position);
            var cardBounds = cardBackSprite.bounds;
            var cardScale = view.Transform.localScale.x;

            for (var index = 0; index < ResidualGrainCount; index++)
            {
                var unscaledOffset = RandomCardBackOffset(cardBounds, out var normalizedX, out var normalizedY);
                var localPosition = start + new Vector3(unscaledOffset.x * cardScale, unscaledOffset.y * cardScale, 0f);
                var grainObject = new GameObject("Daily Card Back Residual Grain");
                grainObject.transform.SetParent(effectRoot, false);
                grainObject.transform.localPosition = localPosition;

                var renderer = grainObject.AddComponent<SpriteRenderer>();
                renderer.sprite = starParticleSprite;
                var color = SampleCardBackDustColor(unscaledOffset, cardBounds);
                color = Color.Lerp(color, Color.white, 0.18f);
                color.a *= UnityEngine.Random.Range(0.88f, 1f);
                var invisibleColor = color;
                invisibleColor.a = 0f;
                renderer.color = invisibleColor;
                renderer.sortingOrder = 2320 + UnityEngine.Random.Range(0, 190);

                var grainScale = UnityEngine.Random.value < 0.66f
                    ? UnityEngine.Random.Range(0.09f, 0.16f)
                    : UnityEngine.Random.Range(0.16f, 0.3f);
                grainObject.transform.localScale = Vector3.one * grainScale;

                var peelCoordinate = GetPeelCoordinate(normalizedX, normalizedY);
                var raggedEdge =
                    Mathf.Sin((normalizedY * 13.8f + normalizedX * 5.7f + UnityEngine.Random.value * 2.8f) * Mathf.PI) * 0.16f +
                    Mathf.Sin((normalizedY * 36.5f + normalizedX * 9.4f) * Mathf.PI) * 0.045f;
                var releaseDelay = baseDelay + Smooth01(peelCoordinate) * 1.68f + raggedEdge + UnityEngine.Random.Range(0f, 0.24f);
                var wind = new Vector3(
                    UnityEngine.Random.Range(1.75f, 4.25f),
                    UnityEngine.Random.Range(-0.82f, -0.08f),
                    0f);
                var turbulence = new Vector3(
                    UnityEngine.Random.Range(-0.64f, 0.76f),
                    UnityEngine.Random.Range(-0.48f, 0.5f),
                    0f);

                grains.Add(new CardBackResidualGrain(
                    grainObject.transform,
                    renderer,
                    localPosition,
                    color,
                    Mathf.Max(0f, releaseDelay),
                    UnityEngine.Random.Range(1.18f, 2.05f),
                    wind,
                    turbulence,
                    UnityEngine.Random.Range(0f, Mathf.PI * 2f)));
            }
        }

        private static void UpdateCardBackResidualGrains(List<CardBackResidualGrain> grains, float elapsed)
        {
            foreach (var grain in grains)
            {
                if (grain.Transform == null || grain.Renderer == null)
                {
                    continue;
                }

                var materialize = Smooth01(Mathf.InverseLerp(grain.ReleaseDelay - 0.22f, grain.ReleaseDelay + 0.02f, elapsed));
                var loosen = Smooth01(Mathf.InverseLerp(grain.ReleaseDelay - 0.18f, grain.ReleaseDelay + 0.06f, elapsed));
                var releaseLinear = Mathf.Clamp01(Mathf.InverseLerp(grain.ReleaseDelay, grain.ReleaseDelay + grain.Duration, elapsed));
                var release = Smooth01(releaseLinear);
                var tremble = new Vector3(
                    Mathf.Sin(elapsed * 15.5f + grain.Phase) * 0.026f,
                    Mathf.Cos(elapsed * 12.8f + grain.Phase) * 0.02f,
                    0f) * loosen;
                var gust = new Vector3(
                    Mathf.Sin(releaseLinear * 18f + grain.Phase) * grain.Turbulence.x,
                    Mathf.Cos(releaseLinear * 14f + grain.Phase) * grain.Turbulence.y,
                    0f);
                var driftEase = Mathf.Pow(releaseLinear, 0.74f);
                var fall = Vector3.down * (0.46f * releaseLinear * releaseLinear);
                grain.Transform.localPosition = grain.StartPosition + tremble + (grain.Wind * driftEase) + (gust * release) + fall;
                grain.Transform.localScale = grain.StartScale * Mathf.Lerp(1f, 0.22f, release);

                var color = grain.BaseColor;
                color.a = grain.BaseColor.a * materialize * (1f - Smooth01(Mathf.InverseLerp(0.55f, 1f, release)));
                grain.Renderer.color = color;
            }
        }

        private static void DestroyCardBackResidualGrains(List<CardBackResidualGrain> grains)
        {
            foreach (var grain in grains)
            {
                if (grain.Transform != null)
                {
                    Destroy(grain.Transform.gameObject);
                }
            }
        }

        private static Vector2 RandomCardBackOffset(Bounds cardBounds, out float normalizedX, out float normalizedY)
        {
            normalizedX = UnityEngine.Random.value;
            normalizedY = UnityEngine.Random.value;

            if (UnityEngine.Random.value < 0.38f)
            {
                var clusterX = UnityEngine.Random.value;
                var clusterY = UnityEngine.Random.value;
                normalizedX = Mathf.Clamp01(clusterX + UnityEngine.Random.Range(-0.14f, 0.14f));
                normalizedY = Mathf.Clamp01(clusterY + UnityEngine.Random.Range(-0.18f, 0.18f));
            }

            normalizedX = Mathf.Clamp01(normalizedX + Mathf.Sin((normalizedY * 7.3f + UnityEngine.Random.value * 2.4f) * Mathf.PI) * 0.018f);
            normalizedY = Mathf.Clamp01(normalizedY + Mathf.Sin((normalizedX * 5.9f + UnityEngine.Random.value * 2.1f) * Mathf.PI) * 0.018f);

            return new Vector2(
                Mathf.Lerp(cardBounds.min.x, cardBounds.max.x, normalizedX),
                Mathf.Lerp(cardBounds.min.y, cardBounds.max.y, normalizedY));
        }

        private static float GetPeelCoordinate(float normalizedX, float normalizedY)
        {
            return Mathf.Clamp01(normalizedX * 0.58f + (1f - normalizedY) * 0.42f);
        }

        private void SpawnWindDust(Vector3 localStart, float delay, float duration, Color litColor, bool isImpact)
        {
            if (effectRoot == null || starParticleSprite == null)
            {
                return;
            }

            var particle = new GameObject("Daily Wind Dust");
            particle.transform.SetParent(effectRoot, false);
            particle.transform.localPosition = localStart;

            var renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = starParticleSprite;
            litColor = Color.Lerp(litColor, Color.white, isImpact ? 0.34f : 0.24f);
            litColor.a = Mathf.Min(1f, litColor.a * (isImpact ? 1.32f : 1.16f));
            var invisibleColor = litColor;
            invisibleColor.a = 0f;
            renderer.color = invisibleColor;
            renderer.sortingOrder = (isImpact ? 2460 : 2400) + UnityEngine.Random.Range(0, 140);

            var startScale = isImpact
                ? UnityEngine.Random.Range(0.18f, 0.36f)
                : UnityEngine.Random.Range(0.1f, 0.22f);
            particle.transform.localScale = Vector3.one * startScale;
            StartCoroutine(AnimateWindDust(particle.transform, renderer, delay, duration, startScale, litColor, isImpact));
        }

        private static IEnumerator AnimateWindDust(
            Transform particle,
            SpriteRenderer renderer,
            float delay,
            float duration,
            float startScale,
            Color litColor,
            bool isImpact)
        {
            var start = particle.localPosition;
            var wind = isImpact
                ? new Vector3(UnityEngine.Random.Range(3.15f, 5.55f), UnityEngine.Random.Range(-1.38f, -0.38f), 0f)
                : new Vector3(UnityEngine.Random.Range(1.9f, 4.1f), UnityEngine.Random.Range(-0.86f, -0.16f), 0f);
            var turbulence = isImpact
                ? new Vector3(UnityEngine.Random.Range(-0.78f, 0.88f), UnityEngine.Random.Range(-0.56f, 0.58f), 0f)
                : new Vector3(UnityEngine.Random.Range(-0.52f, 0.66f), UnityEngine.Random.Range(-0.42f, 0.44f), 0f);
            var phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            var lift = isImpact ? UnityEngine.Random.Range(-0.52f, -0.12f) : UnityEngine.Random.Range(-0.34f, -0.04f);
            var elapsed = 0f;

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            while (elapsed < duration && particle != null)
            {
                elapsed += Time.deltaTime;
                var linearT = Mathf.Clamp01(elapsed / duration);
                var t = Smooth01(linearT);
                var gust = new Vector3(
                    Mathf.Sin(linearT * (isImpact ? 20f : 16f) + phase) * turbulence.x,
                    Mathf.Cos(linearT * (isImpact ? 17f : 13f) + phase) * turbulence.y,
                    0f);
                var drift = wind * Mathf.Pow(linearT, isImpact ? 0.68f : 0.86f) + Vector3.up * (lift * linearT * linearT);
                particle.localPosition = start + drift + gust;
                particle.localScale = Vector3.one * Mathf.Lerp(startScale, startScale * (isImpact ? 0.28f : 0.18f), t);
                var color = litColor;
                var fadeIn = Smooth01(Mathf.InverseLerp(0f, isImpact ? 0.055f : 0.085f, t));
                var fadeOut = 1f - Smooth01(Mathf.InverseLerp(isImpact ? 0.62f : 0.58f, 1f, t));
                color.a = Mathf.Min(1f, litColor.a * (isImpact ? 1.34f : 1.2f) * fadeIn * fadeOut);
                renderer.color = color;
                yield return null;
            }

            if (particle != null)
            {
                Destroy(particle.gameObject);
            }
        }

        private void ClearEffectParticles()
        {
            if (effectRoot == null)
            {
                return;
            }

            for (var index = effectRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(effectRoot.GetChild(index).gameObject);
            }
        }

        private Color SampleCardBackDustColor(Vector2 spriteLocalOffset, Bounds spriteBounds)
        {
            if (cardBackSprite == null || cardBackSprite.texture == null)
            {
                return new Color(0.66f, 0.82f, 1f, 0.84f);
            }

            var texture = cardBackSprite.texture;
            var rect = cardBackSprite.textureRect;
            var normalizedX = Mathf.InverseLerp(spriteBounds.min.x, spriteBounds.max.x, spriteLocalOffset.x);
            var normalizedY = Mathf.InverseLerp(spriteBounds.min.y, spriteBounds.max.y, spriteLocalOffset.y);
            var textureX = Mathf.Lerp(rect.xMin, rect.xMax, normalizedX) / texture.width;
            var textureY = Mathf.Lerp(rect.yMin, rect.yMax, normalizedY) / texture.height;
            var sampled = texture.GetPixelBilinear(textureX, textureY);
            var luminance = sampled.r * 0.2126f + sampled.g * 0.7152f + sampled.b * 0.0722f;

            if (luminance < 0.18f)
            {
                sampled = Color.Lerp(sampled, new Color(0.42f, 0.48f, 0.72f, 1f), 0.68f);
            }
            else
            {
                sampled = Color.Lerp(sampled, Color.white, 0.16f);
            }

            sampled.a = Mathf.Lerp(0.86f, 1f, Mathf.Clamp01(luminance * 1.7f));
            return sampled;
        }

        private Color SampleCardBackExactColor(Vector2 spriteLocalOffset, Bounds spriteBounds)
        {
            if (cardBackSprite == null || cardBackSprite.texture == null)
            {
                return new Color(0.66f, 0.82f, 1f, 1f);
            }

            var texture = cardBackSprite.texture;
            var rect = cardBackSprite.textureRect;
            var normalizedX = Mathf.InverseLerp(spriteBounds.min.x, spriteBounds.max.x, spriteLocalOffset.x);
            var normalizedY = Mathf.InverseLerp(spriteBounds.min.y, spriteBounds.max.y, spriteLocalOffset.y);
            var textureX = Mathf.Lerp(rect.xMin, rect.xMax, normalizedX) / texture.width;
            var textureY = Mathf.Lerp(rect.yMin, rect.yMax, normalizedY) / texture.height;
            var sampled = texture.GetPixelBilinear(textureX, textureY);
            sampled.a = Mathf.Max(sampled.a, 0.96f);
            return sampled;
        }

        private string GetLocalizedCardName(TarotRuntimeCard card)
        {
            return currentLocale == LocaleId.English ? card.EnglishName : card.ChineseName;
        }

        private string FormatResultCardName(string cardName)
        {
            if (currentLocale == LocaleId.English)
            {
                return cardName.ToUpperInvariant();
            }

            return cardName;
        }

        private string GetLocalizedOrientation(TarotOrientation orientation)
        {
            if (currentLocale == LocaleId.English)
            {
                return orientation == TarotOrientation.Upright ? "Upright" : "Reversed";
            }

            return orientation == TarotOrientation.Upright ? "正位" : "逆位";
        }

        private string GetLocalizedKeywordsLabel()
        {
            return currentLocale == LocaleId.English ? "Keywords" : "今日关键词";
        }

        private string GetLocalizedReminderLabel()
        {
            return currentLocale == LocaleId.English ? "Reminder" : "今日提醒";
        }

        private static string GetDailyKeywords(TarotRuntimeCard card, TarotOrientation orientation)
        {
            var baseKeywords = card.ArcanaType == ArcanaType.Major
                ? GetMajorKeywords(card.Number)
                : GetMinorKeywords(card.Suit, card.Number);

            return orientation == TarotOrientation.Upright
                ? baseKeywords
                : $"{baseKeywords}、放慢";
        }

        private static string GetDailyReminder(TarotRuntimeCard card, TarotOrientation orientation)
        {
            return card.ArcanaType == ArcanaType.Major
                ? GetMajorReminder(card.Number, orientation)
                : GetMinorReminder(card.Suit, card.Number, orientation);
        }

        private static string GetMajorKeywords(int number)
        {
            return number switch
            {
                0 => "开始、直觉、轻盈",
                1 => "行动、表达、掌控",
                2 => "倾听、秘密、内在",
                3 => "滋养、创造、丰盛",
                4 => "秩序、边界、稳定",
                5 => "学习、传统、指引",
                6 => "关系、选择、靠近",
                7 => "推进、意志、胜利",
                8 => "勇气、温柔、克制",
                9 => "独处、洞察、沉淀",
                10 => "变化、机会、转向",
                11 => "公平、判断、平衡",
                12 => "等待、换位、暂停",
                13 => "结束、更新、释然",
                14 => "调和、修复、节奏",
                15 => "欲望、束缚、清醒",
                16 => "冲击、真相、重建",
                17 => "希望、疗愈、远方",
                18 => "梦境、迷雾、敏感",
                19 => "明朗、生命力、坦率",
                20 => "回应、召唤、复盘",
                21 => "完成、整合、抵达",
                _ => "觉察、选择、节奏"
            };
        }

        private static string GetMajorReminder(int number, TarotOrientation orientation)
        {
            if (orientation == TarotOrientation.Reversed)
            {
                return number switch
                {
                    0 => "今天先别急着跳出去，确认脚下的路再开始。",
                    1 => "你有工具，但别把所有事都揽到自己身上。",
                    2 => "答案可能还没到公开的时候，先保护自己的感受。",
                    3 => "照顾别人之前，也给自己留一点空间。",
                    4 => "规则可以保护你，但别让它变成僵硬的墙。",
                    5 => "建议值得听，但最后要回到你的真实判断。",
                    6 => "关系里的犹豫需要被看见，不必强行给出答案。",
                    7 => "推进前先校准方向，快不一定代表对。",
                    8 => "温柔不是退让，今天要把力量用得更细致。",
                    9 => "别把独处变成隔绝，必要时可以求助。",
                    10 => "变化还在酝酿，别因为短暂卡顿就否定机会。",
                    11 => "做决定前再核对一次事实，避免被情绪带偏。",
                    12 => "停下来不是失败，它可能是在帮你换一个角度。",
                    13 => "旧事未必需要立刻切断，但你可以慢慢松手。",
                    14 => "今天别把自己拉太满，恢复节奏比硬撑更重要。",
                    15 => "留意让你上瘾或反复消耗的东西，把选择权拿回来。",
                    16 => "不舒服的真相出现时，先稳住，再处理。",
                    17 => "希望不是立刻变好，而是你还愿意继续靠近光。",
                    18 => "如果今天想太多，先把事实和想象分开放。",
                    19 => "别为了表现积极而压住真实感受。",
                    20 => "复盘可以，但别把自己审判得太重。",
                    21 => "完成之前还有收尾工作，慢一点也没关系。",
                    _ => "今天先放慢，把注意力放回你能确认的小事。"
                };
            }

            return number switch
            {
                0 => "适合轻装开始，给今天留一点探索空间。",
                1 => "把想法变成一个小动作，你会更有掌控感。",
                2 => "多听少说，直觉会在安静里给你提示。",
                3 => "适合照顾身体、整理环境，或创造一点美的东西。",
                4 => "先定边界和优先级，稳定感会自然回来。",
                5 => "向可靠的人、书或经验请教，会少走弯路。",
                6 => "今天的重点是选择，也可能是认真面对一段关系。",
                7 => "适合推进计划，但记得握稳方向盘。",
                8 => "用温柔的方式坚持自己，会比硬碰硬更有效。",
                9 => "给自己一点独处时间，答案会慢慢浮上来。",
                10 => "留意临时变化，里面可能藏着新的入口。",
                11 => "把事情说清楚、分清责任，今天会轻松很多。",
                12 => "暂停一下，换个视角看问题会有新发现。",
                13 => "适合整理、告别、更新，让空间重新流动。",
                14 => "保持中庸和耐心，今天适合修复而不是冲刺。",
                15 => "看见欲望背后的需求，你就不会被它牵着走。",
                16 => "突发变化未必是坏事，它可能在拆掉不稳的结构。",
                17 => "给未来一点信任，今天适合做疗愈和补能的事。",
                18 => "情绪敏感时先别急着下结论，让夜色沉一沉。",
                19 => "适合坦率表达、晒太阳、见朋友，能量会被点亮。",
                20 => "回应心里的召唤，今天适合复盘和重新决定。",
                21 => "某件事正在走向完整，记得承认自己的进步。",
                _ => "把注意力放回你能掌控的小事，答案会慢慢变清楚。"
            };
        }

        private static string GetMinorKeywords(TarotSuit suit, int number)
        {
            var suitKeywords = suit switch
            {
                TarotSuit.Wands => "行动、热情",
                TarotSuit.Cups => "感受、关系",
                TarotSuit.Swords => "思考、沟通",
                TarotSuit.Pentacles => "现实、资源",
                _ => "日常、节奏"
            };

            var numberKeyword = number switch
            {
                1 => "萌芽",
                2 => "选择",
                3 => "协作",
                4 => "稳定",
                5 => "摩擦",
                6 => "流动",
                7 => "评估",
                8 => "推进",
                9 => "积累",
                10 => "完成",
                11 => "学习",
                12 => "出发",
                13 => "成熟",
                14 => "掌握",
                _ => "调整"
            };

            return $"{suitKeywords}、{numberKeyword}";
        }

        private static string GetMinorReminder(TarotSuit suit, int number, TarotOrientation orientation)
        {
            var upright = suit switch
            {
                TarotSuit.Wands => number switch
                {
                    1 => "今天适合点燃一个新想法，先做最小的一步。",
                    2 => "把选择摊开看，别让热情替你做全部决定。",
                    3 => "你已经走出一段距离，可以开始看更远的机会。",
                    4 => "适合庆祝小成果，也适合让自己放松一下。",
                    5 => "有摩擦时先别急着赢，找到真正的问题更重要。",
                    6 => "认可会带来动力，但别忘了继续往前走。",
                    7 => "守住你的立场，今天不必讨好所有人。",
                    8 => "消息和进展会变快，保持清醒地接住它。",
                    9 => "你已经撑了很久，今天要保护好自己的精力。",
                    10 => "任务过重时要拆分，不要一个人扛完整座山。",
                    11 => "保持好奇，今天适合尝试新方法。",
                    12 => "行动前先找准目标，冲劲会更有价值。",
                    13 => "用稳定的热情推进事情，别被一时情绪带跑。",
                    14 => "把能量用在真正重要的地方，你会更有影响力。",
                    _ => "把热情落到行动里，今天会更有方向感。"
                },
                TarotSuit.Cups => number switch
                {
                    1 => "让感受自然流动，今天适合表达善意。",
                    2 => "关系里的互相看见，会带来柔软的答案。",
                    3 => "适合和朋友连接，轻松的交流会补充能量。",
                    4 => "如果感到无聊，试着看见身边已经存在的礼物。",
                    5 => "遗憾值得被承认，但别只盯着失去的部分。",
                    6 => "旧回忆可能带来安慰，也提醒你温柔对待自己。",
                    7 => "选择太多时，回到真正让你安心的那个。",
                    8 => "离开消耗你的情绪场，是一种成熟的照顾。",
                    9 => "今天适合满足一个小心愿，让自己被滋养。",
                    10 => "珍惜稳定的情感支持，它会让一天更完整。",
                    11 => "保持敏感和真诚，别害怕表达喜欢。",
                    12 => "温柔靠近之前，也要确认对方是否愿意接住。",
                    13 => "用成熟的方式照顾情绪，别急着评判自己。",
                    14 => "情感稳定时，你会更容易给别人安全感。",
                    _ => "把感受说清楚，关系会更轻一点。"
                },
                TarotSuit.Swords => number switch
                {
                    1 => "一个清晰判断会帮你切开混乱。",
                    2 => "犹豫时先补足信息，别逼自己立刻决定。",
                    3 => "刺痛感需要被照顾，今天别用理性压掉情绪。",
                    4 => "暂停和休息会让思路重新变清楚。",
                    5 => "争论不一定带来胜利，留一点余地更聪明。",
                    6 => "适合离开旧问题，向更平静的方向移动。",
                    7 => "保持机敏，但别让防备变成孤立。",
                    8 => "困住你的可能是想法，不一定是现实。",
                    9 => "焦虑出现时，把问题写下来会比反复想更有用。",
                    10 => "某个想法已经走到尽头，可以停止折磨自己。",
                    11 => "先观察再表达，今天适合收集线索。",
                    12 => "行动要快，但话出口前仍要留一秒判断。",
                    13 => "用清晰和诚实沟通，别绕太多弯。",
                    14 => "今天适合做决策，但要避免太锋利。",
                    _ => "把事实和情绪分开，你会更清醒。"
                },
                TarotSuit.Pentacles => number switch
                {
                    1 => "新的现实机会出现时，先把它稳稳接住。",
                    2 => "今天要管理节奏，别让多个任务互相拉扯。",
                    3 => "合作和专业会带来进展，别单打独斗。",
                    4 => "稳定很重要，但别把安全感变成紧抓不放。",
                    5 => "如果觉得匮乏，先寻找可以求助的资源。",
                    6 => "给予和接受都要平衡，别让关系失衡。",
                    7 => "耐心等待成果，今天适合评估投入是否值得。",
                    8 => "重复练习会带来真实进步，别小看基本功。",
                    9 => "承认自己的积累，今天适合享受一点成果。",
                    10 => "关注长期稳定，家庭、资产或责任会成为重点。",
                    11 => "保持学习心态，现实会奖励认真打磨的人。",
                    12 => "稳健推进，比一时冲动更适合今天。",
                    13 => "用可靠和耐心处理事务，会得到更稳的结果。",
                    14 => "今天适合做资源管理和长期规划。",
                    _ => "把注意力放在现实可执行的一步。"
                },
                _ => "今天适合照顾具体生活，把事情一件件放回位置。"
            };

            if (orientation == TarotOrientation.Upright)
            {
                return upright;
            }

            return suit switch
            {
                TarotSuit.Wands => "别急着用力，先确认这件事是否真的值得你的能量。",
                TarotSuit.Cups => "情绪需要空间，今天别为了和谐而忽略自己的感受。",
                TarotSuit.Swords => "脑中声音太多时，先停下争辩，回到事实本身。",
                TarotSuit.Pentacles => "现实压力可以拆小处理，别让焦虑吞掉行动力。",
                _ => "今天先放慢一点，照顾好自己的节奏。"
            };
        }

        private static Sprite CreateStarParticleSprite()
        {
            const int size = 18;
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
                    var core = Mathf.Clamp01(1f - distance * 6.8f);
                    var shoulder = Mathf.Clamp01(1f - distance * 3.2f);
                    var alpha = Mathf.Clamp01(core + shoulder * shoulder * 0.42f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Material CreateCardDustMeshMaterial(Sprite particleSprite)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            if (particleSprite != null && particleSprite.texture != null)
            {
                material.mainTexture = particleSprite.texture;
            }

            return material;
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
                    var outerBorder = x < 4 || x >= width - 4 || y < 4 || y >= height - 4;
                    var innerVertical = (x >= 18 && x < 20 || x >= width - 20 && x < width - 18) && y >= 14 && y < height - 14;
                    var innerHorizontal = (y >= 14 && y < 16 || y >= height - 16 && y < height - 14) && x >= 18 && x < width - 18;
                    var color = outerBorder || innerVertical || innerHorizontal ? line : fill;

                    if (drawBack)
                    {
                        var dx = x - width * 0.5f;
                        var dy = y - height * 0.5f;
                        var distance = Mathf.Sqrt(dx * dx + dy * dy);
                        var u = x / (float)(width - 1);
                        var v = y / (float)(height - 1);
                        var radial = Mathf.Clamp01(distance / 116f);
                        var blue = new Color(0.045f, 0.1f, 0.22f, 1f);
                        var violet = new Color(0.17f, 0.08f, 0.26f, 1f);
                        var teal = new Color(0.09f, 0.34f, 0.42f, 1f);
                        var gold = new Color(0.92f, 0.72f, 0.32f, 1f);
                        var softGold = new Color(0.72f, 0.52f, 0.24f, 1f);
                        var baseColor = Color.Lerp(blue, violet, Mathf.Clamp01(v * 0.78f + radial * 0.22f));
                        var aura = Mathf.Sin((u + v) * Mathf.PI * 3.2f) * 0.5f + 0.5f;
                        color = Color.Lerp(baseColor, teal, aura * 0.24f);

                        var outerDiamond = Mathf.Abs(Mathf.Abs(dx) / 62f + Mathf.Abs(dy) / 104f - 1f) < 0.026f;
                        var innerDiamond = Mathf.Abs(Mathf.Abs(dx) / 34f + Mathf.Abs(dy) / 58f - 1f) < 0.03f;
                        var centerRing = distance > 25f && distance < 30f;
                        var outerRing = distance > 53f && distance < 56f;
                        var verticalRay = Mathf.Abs(dx) < 1.8f && Mathf.Abs(dy) < 92f;
                        var horizontalRay = Mathf.Abs(dy) < 1.8f && Mathf.Abs(dx) < 54f;
                        var diagonalRay = Mathf.Abs(Mathf.Abs(dy) - Mathf.Abs(dx) * 1.45f) < 1.8f && distance < 72f;
                        var cornerSpark = Mathf.Sin((x * 0.23f + y * 0.17f) * Mathf.PI) > 0.96f && radial > 0.52f;

                        if (outerBorder || innerVertical || innerHorizontal || outerDiamond || centerRing)
                        {
                            color = gold;
                        }
                        else if (innerDiamond || outerRing || verticalRay || horizontalRay || diagonalRay)
                        {
                            color = Color.Lerp(softGold, gold, 0.45f);
                        }
                        else if (cornerSpark)
                        {
                            color = Color.Lerp(teal, gold, 0.28f);
                        }
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 180f);
        }

        private sealed class CardBackParticleBatch
        {
            public CardBackParticleBatch(
                Transform transform,
                Mesh mesh,
                CardBackMeshParticle[] particles,
                Vector3[] vertices,
                Color[] colors)
            {
                Transform = transform;
                Mesh = mesh;
                Particles = particles;
                Vertices = vertices;
                Colors = colors;
            }

            public Transform Transform { get; }
            public Mesh Mesh { get; }
            public CardBackMeshParticle[] Particles { get; }
            public Vector3[] Vertices { get; }
            public Color[] Colors { get; }
        }

        private readonly struct CardBackParticleTemplate
        {
            public CardBackParticleTemplate(
                Vector2 offset,
                float startScale,
                Color baseColor,
                float releaseOffset,
                float duration,
                Vector3 drift,
                Vector3 turbulence,
                float phase,
                bool isImpact)
            {
                Offset = offset;
                StartScale = startScale;
                BaseColor = baseColor;
                ReleaseOffset = releaseOffset;
                Duration = duration;
                Drift = drift;
                Turbulence = turbulence;
                Phase = phase;
                IsImpact = isImpact;
            }

            public Vector2 Offset { get; }
            public float StartScale { get; }
            public Color BaseColor { get; }
            public float ReleaseOffset { get; }
            public float Duration { get; }
            public Vector3 Drift { get; }
            public Vector3 Turbulence { get; }
            public float Phase { get; }
            public bool IsImpact { get; }
        }

        private readonly struct CardBackMeshParticle
        {
            public CardBackMeshParticle(
                Vector3 startPosition,
                float startScale,
                Color baseColor,
                float releaseDelay,
                float duration,
                Vector3 wind,
                Vector3 turbulence,
                float phase,
                bool isImpact)
            {
                StartPosition = startPosition;
                StartScale = startScale;
                BaseColor = baseColor;
                ReleaseDelay = releaseDelay;
                Duration = duration;
                Wind = wind;
                Turbulence = turbulence;
                Phase = phase;
                IsImpact = isImpact;
            }

            public Vector3 StartPosition { get; }
            public float StartScale { get; }
            public Color BaseColor { get; }
            public float ReleaseDelay { get; }
            public float Duration { get; }
            public Vector3 Wind { get; }
            public Vector3 Turbulence { get; }
            public float Phase { get; }
            public bool IsImpact { get; }
        }

        private readonly struct CardBackResidualGrain
        {
            public CardBackResidualGrain(
                Transform transform,
                SpriteRenderer renderer,
                Vector3 startPosition,
                Color baseColor,
                float releaseDelay,
                float duration,
                Vector3 wind,
                Vector3 turbulence,
                float phase)
            {
                Transform = transform;
                Renderer = renderer;
                StartPosition = startPosition;
                StartScale = transform.localScale;
                BaseColor = baseColor;
                ReleaseDelay = releaseDelay;
                Duration = duration;
                Wind = wind;
                Turbulence = turbulence;
                Phase = phase;
            }

            public Transform Transform { get; }
            public SpriteRenderer Renderer { get; }
            public Vector3 StartPosition { get; }
            public Vector3 StartScale { get; }
            public Color BaseColor { get; }
            public float ReleaseDelay { get; }
            public float Duration { get; }
            public Vector3 Wind { get; }
            public Vector3 Turbulence { get; }
            public float Phase { get; }
        }

    }
}
