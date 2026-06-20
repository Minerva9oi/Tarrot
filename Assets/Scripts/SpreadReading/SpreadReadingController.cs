using System;
using System.Collections;
using System.Collections.Generic;
using Tarot.Appearance;
using Tarot.Cards;
using Tarot.Readings;
using Tarot.RuntimeDeck;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tarot.SpreadReading
{
    public sealed class SpreadReadingController : MonoBehaviour
    {
        private const float RevealedCardScale = 1.36f;
        private const float MoveDuration = 0.58f;
        private const float StagedFocusMoveDuration = 0.42f;
        private const float StagedCollectMoveDuration = 0.36f;
        private const float StagedFocusHoldDuration = 0.36f;
        private const float StagedParticleFlowDuration = 2.05f;
        private const float StagedBlackoutFadeDuration = 0.3f;
        private const float StagedSettleDuration = 0.74f;
        private const float InfoPinDelay = 3f;
        private const int ConvergeParticleCount = 360;
        private const int StagedFaceParticleCount = 4800;
        private const int StagedStarParticleCount = 520;
        private const int DeckDissolveParticleCount = 150;
        private const float StagedFocusScaleMultiplier = 1.32f;
        private const bool EnableMagicCircleEffect = false;

        [SerializeField] private BackgroundManager backgroundManager;
        [SerializeField] private Color cardBackColor = Color.white;
        [SerializeField] private Color cardFaceColor = new(0.86f, 0.84f, 0.78f, 1f);
        [SerializeField] private Color cardLineColor = new(0.78f, 0.68f, 0.48f, 1f);
        [SerializeField] private Color cardDimColor = new(0.42f, 0.44f, 0.52f, 0.88f);
        [SerializeField] private Color focusColor = new(1f, 0.92f, 0.68f, 1f);

        private readonly List<SpreadDraw> drawnCards = new();
        private readonly List<StagedDraw> stagedDraws = new();
        private readonly Dictionary<Sprite, Texture2D> readableSpriteCache = new();
        private SpreadDefinition spreadDefinition = SpreadDefinitionCatalog.GetDefault();
        private Font defaultFont;
        private Canvas canvas;
        private GameObject questionPanel;
        private InputField questionInput;
        private Text[] cardResultTexts = Array.Empty<Text>();
        private Transform[] slotAnchors = Array.Empty<Transform>();
        private CardDrawDeckController deckController;
        private CardDeckArtData cardDeckArt;
        private Sprite cardBackSprite;
        private Sprite fallbackCardFaceSprite;
        private Sprite particleSprite;
        private Sprite blackoutSprite;
        private Material particleMaterial;
        private SpriteRenderer blackoutRenderer;
        private Transform magicCircleRoot;
        private bool isResolving;
        private bool drawStarted;
        private string activeQuestion = string.Empty;
        private RectTransform controlsHotspot;
        private RectTransform controlsPanel;
        private CanvasGroup controlsGroup;
        private float controlsHoverTimer;
        private RectTransform infoHotspot;
        private CanvasGroup infoGroup;
        private Text infoText;
        private Image infoProgressImage;
        private bool infoHovering;
        private bool infoPinned;
        private bool infoDismissedUntilExit;
        private float infoHoverTimer;

        public event Action BackRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Songti SC", "STSong", "Hiragino Mincho ProN", "PingFang SC", "Arial" },
                18);
            cardDeckArt = CardDeckArtData.LoadDefault();
            cardBackSprite = CardBackGalleryCatalog.GetSelectedSprite();
            cardBackSprite ??= cardDeckArt != null && cardDeckArt.CardBackSprite != null
                ? cardDeckArt.CardBackSprite
                : CreateCardSprite(cardBackColor, cardLineColor, true);
            fallbackCardFaceSprite = CreateCardSprite(cardFaceColor, new Color(0.28f, 0.24f, 0.2f, 1f), false);
            particleSprite = CreateParticleSprite();
            blackoutSprite = CreateSolidSprite(Color.white);
            particleMaterial = CreateParticleMaterial(particleSprite);

            EnsureEventSystem();
            BuildScene();
            ApplyResponsiveLayout();
            SetQuestionMode();
            backgroundManager?.SetIdle();
        }

        private void Update()
        {
            ApplyResponsiveLayout();
            HandleQuestionInput();
            UpdateHiddenControls();
            UpdateInfoPanel();
        }

        public void Initialize(SpreadDefinition definition)
        {
            spreadDefinition = definition ?? SpreadDefinitionCatalog.GetDefault();
            if (canvas == null)
            {
                return;
            }

            RebuildSceneForDefinition();
        }

        public void SetBackgroundManager(BackgroundManager manager)
        {
            backgroundManager = manager;
        }

        private void BuildScene()
        {
            slotAnchors = new Transform[spreadDefinition.CardCount];
            for (var index = 0; index < slotAnchors.Length; index++)
            {
                slotAnchors[index] = new GameObject($"Spread Slot {index}").transform;
                slotAnchors[index].SetParent(transform, false);
            }

            BuildDeckController();
            CreateBlackoutOverlay();

            canvas = CreateCanvas();
            CreateQuestionPanel(canvas.transform);
            CreateCardResultTexts(canvas.transform);
            CreateInfoPanel(canvas.transform);
            CreateHiddenControls(canvas.transform);
        }

        private void RebuildSceneForDefinition()
        {
            StopAllCoroutines();
            ClearReadableSpriteCache();
            drawnCards.Clear();
            stagedDraws.Clear();
            isResolving = false;
            drawStarted = false;
            activeQuestion = string.Empty;

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            BuildScene();
            ApplyResponsiveLayout();
            SetQuestionMode();
        }

        private void BuildDeckController()
        {
            var deckObject = new GameObject("Spread Draw Deck");
            deckObject.transform.SetParent(transform, false);
            deckController = deckObject.AddComponent<CardDrawDeckController>();
            deckController.CardSelected += HandleCardSelected;
            deckController.Initialize(
                new CardDrawLayoutProfile(
                    spreadDefinition.RevealFlow == SpreadRevealFlow.StagedReveal ? 1.92f : 1.52f,
                    spreadDefinition.RevealFlow == SpreadRevealFlow.StagedReveal ? 6.45f : 6.1f,
                    spreadDefinition.RevealFlow == SpreadRevealFlow.StagedReveal ? -5.95f : -5.46f,
                    spreadDefinition.RevealFlow == SpreadRevealFlow.StagedReveal ? 38f : 34f,
                    GetTargetCardScale(),
                    new Vector2(0.5f, 0.4f),
                    0.82f),
                TarotRuntimeDeck.Cards,
                cardBackSprite,
                card => cardDeckArt != null ? cardDeckArt.GetFrontSprite(card.CardId) : null,
                cardDimColor,
                focusColor);
        }

        private void CreateBlackoutOverlay()
        {
            var overlayObject = new GameObject("Spread Starflow Blackout");
            overlayObject.transform.SetParent(transform, false);
            blackoutRenderer = overlayObject.AddComponent<SpriteRenderer>();
            blackoutRenderer.sprite = blackoutSprite;
            blackoutRenderer.color = Color.clear;
            blackoutRenderer.sortingOrder = 2320;
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Spread Reading Canvas");
            canvasObject.transform.SetParent(transform, false);

            var createdCanvas = canvasObject.AddComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.sortingOrder = 22;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return createdCanvas;
        }

        private void CreateQuestionPanel(Transform parent)
        {
            questionPanel = new GameObject("Question Modal");
            questionPanel.transform.SetParent(parent, false);

            var panelImage = questionPanel.AddComponent<Image>();
            panelImage.color = new Color(0.012f, 0.015f, 0.022f, 0.94f);
            var panelOutline = questionPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.72f, 0.55f, 0.26f, 0.42f);
            panelOutline.effectDistance = new Vector2(1.8f, -1.8f);

            var panelRect = questionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 12f);
            panelRect.sizeDelta = new Vector2(720f, 190f);

            var title = CreateText(questionPanel.transform, spreadDefinition.Title, 31, FontStyle.Bold, new Color(0.92f, 0.9f, 0.82f, 1f), TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0f, 58f);
            title.rectTransform.sizeDelta = new Vector2(620f, 42f);

            var prompt = CreateText(questionPanel.transform, "输入问题，按 Enter 开始；也可以点击外侧跳过。", 19, FontStyle.Normal, new Color(0.64f, 0.68f, 0.7f, 1f), TextAnchor.MiddleCenter);
            prompt.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            prompt.rectTransform.sizeDelta = new Vector2(620f, 34f);

            questionInput = CreateInputField(questionPanel.transform);
            questionInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -42f);
            questionInput.GetComponent<RectTransform>().sizeDelta = new Vector2(590f, 48f);

            prompt.text = "输入问题，按 Enter 开始；也可以点击外侧跳过。";
        }

        private void CreateCardResultTexts(Transform parent)
        {
            cardResultTexts = new Text[spreadDefinition.CardCount];
            for (var index = 0; index < spreadDefinition.CardCount; index++)
            {
                var text = CreateText(parent, string.Empty, 18, FontStyle.Normal, new Color(0.88f, 0.86f, 0.8f, 1f), TextAnchor.UpperCenter);
                text.supportRichText = true;
                text.lineSpacing = 1.04f;
                text.rectTransform.sizeDelta = new Vector2(260f, 94f);
                text.gameObject.SetActive(false);
                cardResultTexts[index] = text;
            }
        }

        private void CreateInfoPanel(Transform parent)
        {
            var iconObject = new GameObject("Spread Info Icon");
            iconObject.transform.SetParent(parent, false);
            var iconImage = iconObject.AddComponent<Image>();
            iconImage.color = new Color(0.04f, 0.045f, 0.056f, 0.48f);
            infoHotspot = iconImage.rectTransform;
            infoHotspot.anchorMin = new Vector2(1f, 1f);
            infoHotspot.anchorMax = new Vector2(1f, 1f);
            infoHotspot.pivot = new Vector2(0.5f, 0.5f);
            infoHotspot.anchoredPosition = new Vector2(-57f, -53f);
            infoHotspot.sizeDelta = new Vector2(36f, 36f);

            var progressObject = new GameObject("Info Hold Progress");
            progressObject.transform.SetParent(iconObject.transform, false);
            infoProgressImage = progressObject.AddComponent<Image>();
            infoProgressImage.sprite = CreateRingSprite();
            infoProgressImage.type = Image.Type.Filled;
            infoProgressImage.fillMethod = Image.FillMethod.Radial360;
            infoProgressImage.fillOrigin = (int)Image.Origin360.Top;
            infoProgressImage.fillClockwise = true;
            infoProgressImage.fillAmount = 0f;
            infoProgressImage.color = new Color(0.86f, 0.74f, 0.45f, 0.95f);
            infoProgressImage.raycastTarget = false;
            infoProgressImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            infoProgressImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            infoProgressImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            infoProgressImage.rectTransform.anchoredPosition = Vector2.zero;
            infoProgressImage.rectTransform.sizeDelta = new Vector2(42f, 42f);

            var iconText = CreateText(iconObject.transform, "i", 22, FontStyle.Bold, new Color(0.86f, 0.74f, 0.45f, 1f), TextAnchor.MiddleCenter);
            iconText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconText.rectTransform.anchoredPosition = new Vector2(0f, 1.6f);
            iconText.rectTransform.sizeDelta = new Vector2(26f, 30f);

            var hover = iconObject.AddComponent<HoverTarget>();
            hover.Initialize(() => infoHovering = true, () => infoHovering = false);

            var iconButton = iconObject.AddComponent<Button>();
            iconButton.targetGraphic = iconImage;
            iconButton.onClick.AddListener(ToggleInfoPinned);
            var iconColors = iconButton.colors;
            iconColors.normalColor = iconImage.color;
            iconColors.highlightedColor = new Color(0.08f, 0.076f, 0.052f, 0.62f);
            iconColors.pressedColor = new Color(0.12f, 0.1f, 0.055f, 0.72f);
            iconColors.selectedColor = iconColors.normalColor;
            iconColors.fadeDuration = 0.12f;
            iconButton.colors = iconColors;

            var panelObject = new GameObject("Spread Info Panel");
            panelObject.transform.SetParent(parent, false);
            var panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.025f, 0.03f, 0.04f, 0.88f);
            panelImage.raycastTarget = false;
            infoGroup = panelObject.AddComponent<CanvasGroup>();
            infoGroup.alpha = 0f;
            infoGroup.blocksRaycasts = false;
            infoGroup.interactable = false;

            var panelRect = panelImage.rectTransform;
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-36f, -82f);
            panelRect.sizeDelta = new Vector2(430f, 270f);

            infoText = CreateText(panelObject.transform, BuildInfoText(), 17, FontStyle.Normal, new Color(0.86f, 0.86f, 0.8f, 1f), TextAnchor.UpperLeft);
            infoText.rectTransform.anchorMin = Vector2.zero;
            infoText.rectTransform.anchorMax = Vector2.one;
            infoText.rectTransform.offsetMin = new Vector2(20f, 16f);
            infoText.rectTransform.offsetMax = new Vector2(-20f, -16f);
        }

        private void CreateHiddenControls(Transform parent)
        {
            var hotspotObject = new GameObject("Hidden Controls Hotspot");
            hotspotObject.transform.SetParent(parent, false);
            var hotspotImage = hotspotObject.AddComponent<Image>();
            hotspotImage.color = Color.clear;
            hotspotImage.raycastTarget = true;
            controlsHotspot = hotspotImage.rectTransform;
            controlsHotspot.anchorMin = new Vector2(0f, 1f);
            controlsHotspot.anchorMax = new Vector2(0f, 1f);
            controlsHotspot.pivot = new Vector2(0f, 1f);
            controlsHotspot.anchoredPosition = Vector2.zero;
            controlsHotspot.sizeDelta = new Vector2(260f, 150f);

            var panelObject = new GameObject("Hidden Controls Panel");
            panelObject.transform.SetParent(parent, false);
            var panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.025f, 0.03f, 0.04f, 0.84f);
            controlsGroup = panelObject.AddComponent<CanvasGroup>();
            controlsGroup.alpha = 0f;
            controlsGroup.interactable = false;
            controlsGroup.blocksRaycasts = false;

            controlsPanel = panelImage.rectTransform;
            controlsPanel.anchorMin = new Vector2(0f, 1f);
            controlsPanel.anchorMax = new Vector2(0f, 1f);
            controlsPanel.pivot = new Vector2(0f, 1f);
            controlsPanel.anchoredPosition = new Vector2(28f, -24f);
            controlsPanel.sizeDelta = new Vector2(248f, 72f);

            CreateButton(panelObject.transform, "返回", new Vector2(-58f, 0f), new Vector2(96f, 40f), () => BackRequested?.Invoke());
            CreateButton(panelObject.transform, "再抽一次", new Vector2(58f, 0f), new Vector2(112f, 40f), ResetReading);
        }

        private InputField CreateInputField(Transform parent)
        {
            var inputObject = new GameObject("Question Input");
            inputObject.transform.SetParent(parent, false);

            var image = inputObject.AddComponent<Image>();
            image.color = new Color(0.006f, 0.008f, 0.013f, 0.96f);
            var outline = inputObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.68f, 0.42f, 0.58f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            var input = inputObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = CreateText(inputObject.transform, string.Empty, 20, FontStyle.Normal, new Color(0.86f, 0.84f, 0.78f, 1f), TextAnchor.MiddleLeft);
            input.textComponent.rectTransform.anchorMin = Vector2.zero;
            input.textComponent.rectTransform.anchorMax = Vector2.one;
            input.textComponent.rectTransform.offsetMin = new Vector2(16f, 4f);
            input.textComponent.rectTransform.offsetMax = new Vector2(-16f, -4f);

            var placeholder = CreateText(inputObject.transform, "可以留空，例如：这件事接下来会如何发展？", 18, FontStyle.Normal, new Color(0.62f, 0.64f, 0.62f, 0.98f), TextAnchor.MiddleLeft);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(16f, 4f);
            placeholder.rectTransform.offsetMax = new Vector2(-16f, -4f);
            input.placeholder = placeholder;
            return input;
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.045f, 0.048f, 0.06f, 0.72f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.11f, 0.13f, 0.14f, 0.9f);
            colors.pressedColor = new Color(0.16f, 0.14f, 0.11f, 0.96f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.16f;
            button.colors = colors;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = CreateText(buttonObject.transform, label, 19, FontStyle.Normal, new Color(0.9f, 0.88f, 0.8f, 1f), TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private Text CreateText(Transform parent, string value, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return text;
        }

        private void SetQuestionMode()
        {
            drawStarted = false;
            infoPinned = false;
            infoHovering = false;
            infoDismissedUntilExit = false;
            infoHoverTimer = 0f;
            if (infoProgressImage != null)
            {
                infoProgressImage.fillAmount = 0f;
            }
            if (infoGroup != null)
            {
                infoGroup.alpha = 0f;
            }

            deckController.gameObject.SetActive(true);
            deckController.enabled = true;
            deckController.SetInputEnabled(false);
            questionPanel.SetActive(true);
            questionInput.text = string.Empty;
            questionInput.ActivateInputField();
            ClearCardResultTexts();
        }

        private void StartReading()
        {
            if (drawStarted)
            {
                return;
            }

            activeQuestion = questionInput != null ? questionInput.text.Trim() : string.Empty;
            drawStarted = true;
            questionPanel.SetActive(false);
            deckController.SetInputEnabled(true);
        }

        private void HandleQuestionInput()
        {
            if (questionPanel == null || !questionPanel.activeSelf)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                StartReading();
                return;
            }

            if (!UnityEngine.Input.GetMouseButtonDown(0))
            {
                return;
            }

            var panelRect = questionPanel.GetComponent<RectTransform>();
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, UnityEngine.Input.mousePosition, null))
            {
                StartReading();
            }
        }

        private void HandleCardSelected(CardDrawCardView selected)
        {
            if (!drawStarted || isResolving || GetDrawCount() >= spreadDefinition.CardCount)
            {
                return;
            }

            isResolving = true;
            deckController.SetInputEnabled(false);
            var slotIndex = GetDrawCount();
            var orientation = UnityEngine.Random.value > 0.5f ? TarotOrientation.Upright : TarotOrientation.Reversed;
            if (spreadDefinition.RevealFlow == SpreadRevealFlow.ImmediateReveal)
            {
                StartCoroutine(RevealSelectedCard(selected, slotIndex, orientation));
                return;
            }

            StartCoroutine(StageSelectedCard(selected, slotIndex, orientation));
        }

        private IEnumerator RevealSelectedCard(CardDrawCardView selected, int slotIndex, TarotOrientation orientation)
        {
            selected.Collider.enabled = false;
            selected.Renderer.sortingOrder = 2600 + slotIndex;
            var startPosition = selected.Transform.localPosition;
            var targetPosition = slotAnchors[slotIndex].localPosition;
            var startScale = selected.Transform.localScale.x;
            var targetScale = GetTargetCardScale();
            var particleBatch = CreateConvergeParticleBatch(
                startPosition,
                targetPosition,
                startScale,
                targetScale,
                cardBackSprite,
                ConvergeParticleCount,
                0,
                TarotOrientation.Upright);
            var elapsed = 0f;

            while (elapsed < MoveDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / MoveDuration);
                var t = 1f - Mathf.Pow(1f - progress, 3f);
                selected.Transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                selected.Transform.localRotation = Quaternion.identity;
                selected.Transform.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, t);
                UpdateConvergeParticleBatch(particleBatch, progress);
                yield return null;
            }

            DestroyConvergeParticleBatch(particleBatch);
            selected.Transform.localPosition = targetPosition;
            selected.Transform.localScale = Vector3.one * targetScale;
            yield return FlipSelectedCard(selected, orientation, targetScale);

            var slot = spreadDefinition.Slots[slotIndex];
            drawnCards.Add(new SpreadDraw(selected.Card, orientation, slot));
            ShowCardResult(slotIndex, drawnCards[slotIndex]);
            isResolving = false;

            if (drawnCards.Count >= spreadDefinition.CardCount)
            {
                deckController.SetInputEnabled(false);
                yield break;
            }

            deckController.SetInputEnabled(true);
        }

        private IEnumerator StageSelectedCard(CardDrawCardView selected, int slotIndex, TarotOrientation orientation)
        {
            selected.Collider.enabled = false;
            selected.Renderer.sortingOrder = 2600 + slotIndex;
            var startPosition = selected.Transform.localPosition;
            var focusPosition = GetStageFocusLocalPosition();
            var waitingPosition = GetWaitingLocalPosition(slotIndex);
            var startScale = selected.Transform.localScale.x;
            var focusScale = startScale * StagedFocusScaleMultiplier;
            var waitingScale = GetWaitingCardScale();
            var elapsed = 0f;

            while (elapsed < StagedFocusMoveDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / StagedFocusMoveDuration);
                var t = 1f - Mathf.Pow(1f - progress, 3f);
                selected.Transform.localPosition = Vector3.Lerp(startPosition, focusPosition, t);
                selected.Transform.localRotation = Quaternion.identity;
                selected.Transform.localScale = Vector3.one * Mathf.Lerp(startScale, focusScale, t);
                yield return null;
            }

            selected.Transform.localPosition = focusPosition;
            selected.Transform.localRotation = Quaternion.identity;
            selected.Transform.localScale = Vector3.one * focusScale;
            yield return FlipSelectedCard(selected, orientation, focusScale);
            yield return new WaitForSeconds(StagedFocusHoldDuration);
            yield return FlowSelectedCardToWaiting(selected, orientation, focusPosition, waitingPosition, focusScale, waitingScale);

            selected.Transform.localPosition = waitingPosition;
            selected.Transform.localRotation = GetCardRotation(orientation);
            selected.Transform.localScale = Vector3.one * waitingScale;

            var slot = spreadDefinition.Slots[slotIndex];
            stagedDraws.Add(new StagedDraw(selected, new SpreadDraw(selected.Card, orientation, slot), slotIndex));
            isResolving = false;

            if (stagedDraws.Count >= spreadDefinition.CardCount)
            {
                StartCoroutine(CompleteStagedReveal());
                yield break;
            }

            deckController.SetInputEnabled(true);
        }

        private IEnumerator CompleteStagedReveal()
        {
            isResolving = true;
            deckController.SetInputEnabled(false);
            yield return DissolveUndrawnDeckCards();
            deckController.enabled = false;
            yield return MoveStagedCardsToFinalLayout();
            CreateMagicCircleEffect();

            drawnCards.Clear();
            for (var index = 0; index < stagedDraws.Count; index++)
            {
                drawnCards.Add(stagedDraws[index].Draw);
                ShowCardResult(index, stagedDraws[index].Draw);
            }

            isResolving = false;
        }

        private IEnumerator MoveStagedCardsToFinalLayout()
        {
            var finalScale = GetTargetCardScale();
            var count = stagedDraws.Count;
            var startPositions = new Vector3[count];
            var startScales = new float[count];
            for (var index = 0; index < count; index++)
            {
                var view = stagedDraws[index].View;
                startPositions[index] = view.Transform.localPosition;
                startScales[index] = view.Transform.localScale.x;
                view.Renderer.sortingOrder = 2700 + index;
            }

            var elapsed = 0f;
            while (elapsed < StagedSettleDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / StagedSettleDuration);
                var t = Smooth01(progress);
                for (var index = 0; index < count; index++)
                {
                    var view = stagedDraws[index].View;
                    view.Transform.localPosition = Vector3.Lerp(startPositions[index], slotAnchors[index].localPosition, t);
                    view.Transform.localRotation = GetCardRotation(stagedDraws[index].Draw.Orientation);
                    view.Transform.localScale = Vector3.one * Mathf.Lerp(startScales[index], finalScale, t);
                }

                yield return null;
            }

            for (var index = 0; index < count; index++)
            {
                var view = stagedDraws[index].View;
                view.Transform.localPosition = slotAnchors[index].localPosition;
                view.Transform.localRotation = GetCardRotation(stagedDraws[index].Draw.Orientation);
                view.Transform.localScale = Vector3.one * finalScale;
            }
        }

        private IEnumerator FlipStagedCards()
        {
            const float flipDuration = 0.62f;
            var finalScale = GetTargetCardScale();
            var frontApplied = false;
            var elapsed = 0f;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / flipDuration);
                var flipWidth = progress < 0.5f
                    ? Mathf.Lerp(1f, 0.08f, Smooth01(progress * 2f))
                    : Mathf.Lerp(0.08f, 1f, Smooth01((progress - 0.5f) * 2f));

                if (!frontApplied && progress >= 0.5f)
                {
                    for (var index = 0; index < stagedDraws.Count; index++)
                    {
                        ApplyCardFace(stagedDraws[index].View, stagedDraws[index].Draw.Orientation);
                    }

                    frontApplied = true;
                }

                for (var index = 0; index < stagedDraws.Count; index++)
                {
                    stagedDraws[index].View.Transform.localScale = new Vector3(finalScale * flipWidth, finalScale, finalScale);
                }

                yield return null;
            }

            for (var index = 0; index < stagedDraws.Count; index++)
            {
                ApplyCardFace(stagedDraws[index].View, stagedDraws[index].Draw.Orientation);
                stagedDraws[index].View.Transform.localScale = Vector3.one * finalScale;
            }
        }

        private IEnumerator FlipSelectedCard(CardDrawCardView selected, TarotOrientation orientation, float targetScale)
        {
            const float flipDuration = 0.54f;
            var elapsed = 0f;
            var frontApplied = false;

            while (elapsed < flipDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / flipDuration);
                var flipWidth = progress < 0.5f
                    ? Mathf.Lerp(1f, 0.08f, Smooth01(progress * 2f))
                    : Mathf.Lerp(0.08f, 1f, Smooth01((progress - 0.5f) * 2f));

                if (!frontApplied && progress >= 0.5f)
                {
                    ApplyCardFace(selected, orientation);
                    frontApplied = true;
                }

                selected.Transform.localScale = new Vector3(targetScale * flipWidth, targetScale, targetScale);
                yield return null;
            }

            ApplyCardFace(selected, orientation);
            selected.Transform.localScale = Vector3.one * targetScale;
        }

        private IEnumerator FlowSelectedCardToWaiting(
            CardDrawCardView selected,
            TarotOrientation orientation,
            Vector3 focusPosition,
            Vector3 waitingPosition,
            float focusScale,
            float waitingScale)
        {
            var faceSprite = selected.Renderer.sprite != null ? selected.Renderer.sprite : fallbackCardFaceSprite;
            var flowBatch = CreateConvergeParticleBatch(
                focusPosition,
                waitingPosition,
                focusScale,
                waitingScale,
                faceSprite,
                StagedFaceParticleCount,
                StagedStarParticleCount,
                orientation,
                true);

            var baseColor = selected.Renderer.color;
            var elapsed = 0f;
            while (elapsed < StagedParticleFlowDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / StagedParticleFlowDuration);
                var blackoutIn = Smooth01(Mathf.InverseLerp(0.08f, 0.44f, progress));
                var cardFade = 1f - Smooth01(Mathf.InverseLerp(0.12f, 0.42f, progress));
                selected.Renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * cardFade);
                SetBlackoutAlpha(Mathf.Lerp(0f, 0.92f, blackoutIn));
                UpdateConvergeParticleBatch(flowBatch, progress);
                yield return null;
            }

            DestroyConvergeParticleBatch(flowBatch);
            selected.Transform.localPosition = waitingPosition;
            selected.Transform.localRotation = GetCardRotation(orientation);
            selected.Transform.localScale = Vector3.one * waitingScale;
            selected.Renderer.color = baseColor;

            elapsed = 0f;
            while (elapsed < StagedBlackoutFadeDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / StagedBlackoutFadeDuration);
                SetBlackoutAlpha(Mathf.Lerp(0.92f, 0f, Smooth01(progress)));
                yield return null;
            }

            SetBlackoutAlpha(0f);
        }

        private void ApplyCardFace(CardDrawCardView selected, TarotOrientation orientation)
        {
            selected.Renderer.sprite = selected.FrontSprite != null ? selected.FrontSprite : fallbackCardFaceSprite;
            selected.Renderer.color = orientation == TarotOrientation.Upright ? Color.white : new Color(0.92f, 0.9f, 0.96f, 1f);
            selected.Transform.localRotation = GetCardRotation(orientation);
        }

        private static Quaternion GetCardRotation(TarotOrientation orientation)
        {
            return orientation == TarotOrientation.Upright ? Quaternion.identity : Quaternion.Euler(0f, 0f, 180f);
        }

        private void ShowCardResult(int slotIndex, SpreadDraw draw)
        {
            if (slotIndex < 0 || slotIndex >= cardResultTexts.Length)
            {
                return;
            }

            cardResultTexts[slotIndex].gameObject.SetActive(true);
            cardResultTexts[slotIndex].text = FormatDraw(draw);
        }

        private static string FormatDraw(SpreadDraw draw)
        {
            var orientation = draw.Orientation == TarotOrientation.Upright ? "正位" : "逆位";
            return
                $"<b>{draw.Card.ChineseName}</b>  <size=14>{orientation}</size>\n" +
                $"<size=16>{draw.Slot.Name}：{CreatePositionReading(draw)}</size>";
        }

        private static string CreatePositionReading(SpreadDraw draw)
        {
            var tone = draw.Orientation == TarotOrientation.Upright ? "正在顺势展开" : "需要放慢确认";
            return $"{GetDisplaySlotMeaning(draw.Slot)}与{GetCardTheme(draw.Card)}有关，它{tone}。";
        }

        private static string GetCardTheme(TarotRuntimeCard card)
        {
            if (card.ArcanaType == ArcanaType.Major)
            {
                return card.Number switch
                {
                    0 => "新的开始",
                    1 => "行动与掌控",
                    2 => "直觉与隐藏信息",
                    3 => "滋养与创造",
                    4 => "秩序与边界",
                    5 => "学习与指引",
                    6 => "关系与选择",
                    7 => "推进与意志",
                    8 => "力量与温柔",
                    9 => "独处与洞察",
                    10 => "变化与转向",
                    11 => "公平与判断",
                    12 => "暂停与换位",
                    13 => "结束与更新",
                    14 => "调和与修复",
                    15 => "欲望与束缚",
                    16 => "冲击与重建",
                    17 => "希望与疗愈",
                    18 => "梦境与不确定",
                    19 => "明朗与生命力",
                    20 => "召唤与复盘",
                    21 => "完成与整合",
                    _ => "觉察与选择"
                };
            }

            return card.Suit switch
            {
                TarotSuit.Wands => "行动、热情与推进",
                TarotSuit.Cups => "情绪、关系与感受",
                TarotSuit.Swords => "思考、判断与沟通",
                TarotSuit.Pentacles => "现实、资源与稳定",
                _ => "当下的关键线索"
            };
        }

        private void ResetReading()
        {
            StopAllCoroutines();
            ClearConvergeParticles();
            ClearMagicCircle();
            SetBlackoutAlpha(0f);
            drawnCards.Clear();
            stagedDraws.Clear();
            isResolving = false;
            activeQuestion = string.Empty;
            ClearCardResultTexts();
            deckController.enabled = true;
            deckController.ResetDeck();
            SetQuestionMode();
        }

        private void OnDestroy()
        {
            ClearReadableSpriteCache();
        }

        private int GetDrawCount()
        {
            return spreadDefinition.RevealFlow == SpreadRevealFlow.StagedReveal
                ? stagedDraws.Count
                : drawnCards.Count;
        }

        private float GetTargetCardScale()
        {
            if (spreadDefinition.RevealFlow == SpreadRevealFlow.ImmediateReveal)
            {
                return RevealedCardScale;
            }

            return spreadDefinition.CardCount switch
            {
                >= 7 => 1.22f,
                6 => 1.32f,
                5 => 1.46f,
                4 => 1.56f,
                _ => 1.62f
            };
        }

        private float GetStageFocusCardScale()
        {
            return 1.58f;
        }

        private float GetWaitingCardScale()
        {
            return 0.44f;
        }

        private Vector3 GetStageFocusLocalPosition()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                return new Vector3(0f, -0.4f, 0f);
            }

            return ViewportToLocalWorldPoint(mainCamera, new Vector2(0.5f, 0.47f));
        }

        private Vector3 GetWaitingLocalPosition(int slotIndex)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                return new Vector3(-6f + slotIndex * 0.72f, -4.3f, 0f);
            }

            var columns = Mathf.Min(spreadDefinition.CardCount, 4);
            var row = slotIndex / columns;
            var column = slotIndex % columns;
            var rowStart = row * columns;
            var itemsInRow = Mathf.Min(columns, spreadDefinition.CardCount - rowStart);
            var x = 0.5f + (column - (itemsInRow - 1) * 0.5f) * 0.13f;
            var y = 0.14f + row * 0.18f;
            var viewport = new Vector2(x, y);
            return ViewportToLocalWorldPoint(mainCamera, viewport);
        }

        private IEnumerator DissolveUndrawnDeckCards()
        {
            if (deckController == null)
            {
                yield break;
            }

            var batches = new List<ConvergeParticleBatch>();
            var fadeRenderers = new List<SpriteRenderer>();
            var spawned = 0;
            foreach (var view in deckController.CardViews)
            {
                if (view == null || view.IsSelected || !view.Transform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (spawned < 22)
                {
                    var driftTarget = view.Transform.localPosition + new Vector3(
                        UnityEngine.Random.Range(-1.2f, 1.2f),
                        UnityEngine.Random.Range(1.6f, 2.8f),
                        0f);
                    batches.Add(CreateConvergeParticleBatch(
                        view.Transform.localPosition,
                        driftTarget,
                        view.Transform.localScale.x,
                        0.4f,
                        cardBackSprite,
                        DeckDissolveParticleCount,
                        0,
                        TarotOrientation.Upright));
                    spawned++;
                }

                fadeRenderers.Add(view.Renderer);
            }

            const float duration = 0.92f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var fade = 1f - Smooth01(Mathf.InverseLerp(0.02f, 0.36f, progress));
                foreach (var renderer in fadeRenderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    var color = renderer.color;
                    color.a = fade;
                    renderer.color = color;
                }

                foreach (var batch in batches)
                {
                    UpdateConvergeParticleBatch(batch, progress);
                }

                yield return null;
            }

            foreach (var batch in batches)
            {
                DestroyConvergeParticleBatch(batch);
            }

            HideUndrawnDeckCards();
        }

        private void HideUndrawnDeckCards()
        {
            if (deckController == null)
            {
                return;
            }

            foreach (var view in deckController.CardViews)
            {
                if (view == null || view.IsSelected)
                {
                    continue;
                }

                view.Transform.gameObject.SetActive(false);
            }
        }

        private void SetBlackoutAlpha(float alpha)
        {
            if (blackoutRenderer == null)
            {
                return;
            }

            blackoutRenderer.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        }

        private void ClearMagicCircle()
        {
            if (magicCircleRoot == null)
            {
                return;
            }

            Destroy(magicCircleRoot.gameObject);
            magicCircleRoot = null;
        }

        private void CreateMagicCircleEffect()
        {
            ClearMagicCircle();
            if (!EnableMagicCircleEffect)
            {
                return;
            }

            magicCircleRoot = new GameObject("Spread Magic Circle").transform;
            magicCircleRoot.SetParent(transform, false);
            magicCircleRoot.localPosition = Vector3.zero;

            var ring = new GameObject("Magic Circle Ring");
            ring.transform.SetParent(magicCircleRoot, false);
            var renderer = ring.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateMagicCircleSprite();
            renderer.color = new Color(0.88f, 0.72f, 0.42f, 0.26f);
            renderer.sortingOrder = 2380;
            ring.transform.localScale = Vector3.one * 7.4f;
            magicCircleRoot.gameObject.AddComponent<MagicCirclePulse>();
        }

        private void ClearConvergeParticles()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (child.name.IndexOf("Spread Converge Particles", StringComparison.Ordinal) >= 0 ||
                    child.name.IndexOf("Spread Magic Circle", StringComparison.Ordinal) >= 0)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void ClearCardResultTexts()
        {
            foreach (var text in cardResultTexts)
            {
                if (text == null)
                {
                    continue;
                }

                text.text = string.Empty;
                text.gameObject.SetActive(false);
            }
        }

        private void ApplyResponsiveLayout()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic || slotAnchors == null)
            {
                return;
            }

            for (var index = 0; index < spreadDefinition.CardCount; index++)
            {
                var slot = spreadDefinition.Slots[index];
                slotAnchors[index].localPosition = ViewportToLocalWorldPoint(mainCamera, slot.ViewportPosition);
                if (cardResultTexts != null && index < cardResultTexts.Length && cardResultTexts[index] != null)
                {
                    var textPosition = new Vector2((slot.ViewportPosition.x - 0.5f) * 1920f, (slot.ViewportPosition.y - 0.5f) * 1080f - 164f);
                    cardResultTexts[index].rectTransform.anchoredPosition = textPosition;
                }
            }

            if (blackoutRenderer != null)
            {
                var depth = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
                var bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
                var topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
                var center = (bottomLeft + topRight) * 0.5f;
                blackoutRenderer.transform.localPosition = transform.InverseTransformPoint(center);
                blackoutRenderer.transform.localScale = new Vector3(
                    Mathf.Abs(topRight.x - bottomLeft.x) * 1.08f,
                    Mathf.Abs(topRight.y - bottomLeft.y) * 1.08f,
                    1f);
            }
        }

        private void UpdateHiddenControls()
        {
            if (controlsHotspot == null || controlsGroup == null)
            {
                return;
            }

            var isHovering =
                RectTransformUtility.RectangleContainsScreenPoint(controlsHotspot, UnityEngine.Input.mousePosition, null) ||
                RectTransformUtility.RectangleContainsScreenPoint(controlsPanel, UnityEngine.Input.mousePosition, null);
            controlsHoverTimer = isHovering ? controlsHoverTimer + Time.unscaledDeltaTime : 0f;
            var targetAlpha = controlsHoverTimer >= 2f ? 1f : 0f;
            controlsGroup.alpha = Mathf.Lerp(controlsGroup.alpha, targetAlpha, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            var visible = controlsGroup.alpha > 0.12f;
            controlsGroup.interactable = visible;
            controlsGroup.blocksRaycasts = visible;

            if (!isHovering && controlsGroup.alpha < 0.18f)
            {
                controlsGroup.interactable = false;
                controlsGroup.blocksRaycasts = false;
            }
        }

        private void UpdateInfoPanel()
        {
            if (infoGroup == null || infoHotspot == null)
            {
                return;
            }

            var pointerHovering = RectTransformUtility.RectangleContainsScreenPoint(infoHotspot, UnityEngine.Input.mousePosition, null);
            var isHovering = infoHovering || pointerHovering;
            if (!isHovering)
            {
                infoDismissedUntilExit = false;
            }

            if (infoDismissedUntilExit)
            {
                isHovering = false;
            }

            if (isHovering && !infoPinned)
            {
                infoHoverTimer = Mathf.Min(InfoPinDelay, infoHoverTimer + Time.unscaledDeltaTime);
                if (infoHoverTimer >= InfoPinDelay)
                {
                    infoPinned = true;
                }
            }
            else if (!isHovering && !infoPinned)
            {
                infoHoverTimer = 0f;
            }

            var targetAlpha = isHovering || infoPinned ? 1f : 0f;
            infoGroup.alpha = Mathf.Lerp(infoGroup.alpha, targetAlpha, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            var visible = infoGroup.alpha > 0.08f;
            infoGroup.interactable = visible;
            infoGroup.blocksRaycasts = false;

            if (infoProgressImage != null)
            {
                infoProgressImage.fillAmount = infoPinned ? 1f : Mathf.Clamp01(infoHoverTimer / InfoPinDelay);
                infoProgressImage.enabled = isHovering || infoPinned || infoProgressImage.fillAmount > 0.01f;
            }
        }

        private void ToggleInfoPinned()
        {
            if (infoPinned)
            {
                infoPinned = false;
                infoHoverTimer = 0f;
                infoHovering = false;
                infoDismissedUntilExit = true;
                return;
            }

            infoPinned = true;
            infoDismissedUntilExit = false;
            infoHoverTimer = InfoPinDelay;
        }

        private string BuildInfoText()
        {
            var text = $"{spreadDefinition.Title}\n{spreadDefinition.Description}\n\n编号与牌阵预览一致：\n";
            for (var index = 0; index < spreadDefinition.CardCount; index++)
            {
                var slot = spreadDefinition.Slots[index];
                text += $"{index + 1}. {slot.Name}：{GetDisplaySlotMeaning(slot)}\n";
            }

            return text.TrimEnd();
        }

        private static string GetDisplaySlotMeaning(SpreadCardSlotDefinition slot)
        {
            var meaning = slot.Meaning.Trim();
            var normalizedName = NormalizeSlotText(slot.Name);
            var normalizedMeaning = NormalizeSlotText(meaning);

            if (normalizedMeaning.StartsWith(normalizedName, StringComparison.Ordinal))
            {
                return TrimMeaningPrefix(meaning, slot.Name.Length);
            }

            if (normalizedName == "A结果" && normalizedMeaning.StartsWith("选择A的", StringComparison.Ordinal))
            {
                return TrimMeaningPrefix(meaning, "选择 A 的".Length);
            }

            if (normalizedName == "B结果" && normalizedMeaning.StartsWith("选择B的", StringComparison.Ordinal))
            {
                return TrimMeaningPrefix(meaning, "选择 B 的".Length);
            }

            return meaning;
        }

        private static string NormalizeSlotText(string value)
        {
            return value.Replace(" ", string.Empty).Replace("：", string.Empty).Trim();
        }

        private static string TrimMeaningPrefix(string meaning, int count)
        {
            var result = meaning.Length > count ? meaning.Substring(count).Trim() : meaning.Trim();
            while (result.StartsWith("的", StringComparison.Ordinal))
            {
                result = result.Substring(1).Trim();
            }

            return string.IsNullOrEmpty(result) ? meaning : result;
        }

        private Vector3 ViewportToLocalWorldPoint(Camera mainCamera, Vector2 viewportPosition)
        {
            var depth = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            var worldPosition = mainCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, depth));
            worldPosition.z = 0f;
            return transform.InverseTransformPoint(worldPosition);
        }

        private ConvergeParticleBatch CreateConvergeParticleBatch(
            Vector3 startPosition,
            Vector3 targetPosition,
            float cardScale,
            float targetScale,
            Sprite sampleSprite,
            int cardParticleCount,
            int starParticleCount,
            TarotOrientation orientation,
            bool stagedFlow = false)
        {
            if (particleMaterial == null || sampleSprite == null)
            {
                return null;
            }

            var batchObject = new GameObject("Spread Converge Particles");
            batchObject.transform.SetParent(transform, false);

            var particleCount = Mathf.Max(0, cardParticleCount) + Mathf.Max(0, starParticleCount);
            var particles = new ConvergeParticle[particleCount];
            var vertices = new Vector3[particleCount * 4];
            var colors = new Color[particleCount * 4];
            var uvs = new Vector2[particleCount * 4];
            var triangles = new int[particleCount * 6];
            var bounds = sampleSprite.bounds;
            var path = targetPosition - startPosition;
            var side = new Vector3(-path.y, path.x, 0f).normalized;
            if (side.sqrMagnitude < 0.0001f)
            {
                side = Vector3.right;
            }

            for (var index = 0; index < cardParticleCount; index++)
            {
                var localOffset = new Vector2(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x) * cardScale,
                    UnityEngine.Random.Range(bounds.min.y, bounds.max.y) * cardScale);
                if (orientation == TarotOrientation.Reversed)
                {
                    localOffset = -localOffset;
                }

                var start = startPosition + new Vector3(localOffset.x, localOffset.y, 0f);
                var normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, localOffset.x / Mathf.Max(cardScale, 0.01f));
                var normalizedY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, localOffset.y / Mathf.Max(cardScale, 0.01f));
                var edgeDistance = Mathf.Max(
                    Mathf.Abs(normalizedX - 0.5f) * 2f,
                    Mathf.Abs(normalizedY - 0.5f) * 2f);
                var edgeLift = Smooth01(Mathf.InverseLerp(0.42f, 1f, edgeDistance));
                var lift = start + new Vector3(
                    UnityEngine.Random.Range(-0.05f, 0.05f) * cardScale,
                    UnityEngine.Random.Range(0.24f, 0.62f) * cardScale * Mathf.Lerp(0.38f, 1f, edgeLift),
                    0f);
                var control = start + path * UnityEngine.Random.Range(0.48f, 0.58f) + side * UnityEngine.Random.Range(-0.035f, 0.035f);
                var target = targetPosition + new Vector3(
                    UnityEngine.Random.Range(-0.33f, 0.33f) * targetScale,
                    UnityEngine.Random.Range(-0.52f, 0.52f) * targetScale,
                    0f);
                particles[index] = new ConvergeParticle(
                    start,
                    lift,
                    control,
                    target,
                    SampleSpriteColor(sampleSprite, localOffset / Mathf.Max(cardScale, 0.01f), bounds),
                    stagedFlow ? UnityEngine.Random.Range(0.058f, 0.116f) : UnityEngine.Random.Range(0.032f, 0.066f),
                    stagedFlow ? UnityEngine.Random.Range(0f, 0.045f) : UnityEngine.Random.Range(0f, 0.08f),
                    stagedFlow ? UnityEngine.Random.Range(0.58f, 0.98f) : UnityEngine.Random.Range(0.48f, 0.88f),
                    UnityEngine.Random.Range(0f, Mathf.PI * 2f));
                WriteParticleStaticData(index, uvs, triangles);
            }

            for (var index = cardParticleCount; index < particleCount; index++)
            {
                var start = GetRandomScreenLocalPosition();
                var lift = start + new Vector3(UnityEngine.Random.Range(-0.06f, 0.06f), UnityEngine.Random.Range(0.02f, 0.18f), 0f);
                var control = Vector3.Lerp(start, targetPosition, UnityEngine.Random.Range(0.48f, 0.62f)) +
                    new Vector3(UnityEngine.Random.Range(-0.06f, 0.06f), UnityEngine.Random.Range(-0.06f, 0.06f), 0f);
                var color = UnityEngine.Random.value > 0.78f
                    ? new Color(1f, 0.9f, 0.58f, 0.9f)
                    : new Color(0.86f, 0.92f, 1f, 0.82f);
                var target = targetPosition + new Vector3(
                    UnityEngine.Random.Range(-0.42f, 0.42f) * targetScale,
                    UnityEngine.Random.Range(-0.58f, 0.58f) * targetScale,
                    0f);
                particles[index] = new ConvergeParticle(
                    start,
                    lift,
                    control,
                    target,
                    color,
                    stagedFlow ? UnityEngine.Random.Range(0.04f, 0.088f) : UnityEngine.Random.Range(0.022f, 0.052f),
                    stagedFlow ? UnityEngine.Random.Range(0.1f, 0.26f) : UnityEngine.Random.Range(0.04f, 0.22f),
                    stagedFlow ? UnityEngine.Random.Range(0.52f, 0.88f) : UnityEngine.Random.Range(0.36f, 0.72f),
                    UnityEngine.Random.Range(0f, Mathf.PI * 2f));
                WriteParticleStaticData(index, uvs, triangles);
            }

            var mesh = new Mesh { name = "Spread Converge Particles" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(42f, 24f, 2f));

            var meshFilter = batchObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = batchObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = particleMaterial;
            meshRenderer.sortingOrder = 2500;

            return new ConvergeParticleBatch(batchObject, mesh, particles, vertices, colors, stagedFlow);
        }

        private void UpdateConvergeParticleBatch(ConvergeParticleBatch batch, float progress)
        {
            if (batch == null)
            {
                return;
            }

            for (var index = 0; index < batch.Particles.Length; index++)
            {
                var particle = batch.Particles[index];
                var release = Smooth01(Mathf.Clamp01((progress - particle.Delay) / Mathf.Max(0.01f, 1f - particle.Delay)));
                var direction = particle.Target - particle.Start;
                var side = direction.sqrMagnitude > 0.0001f ? new Vector3(-direction.y, direction.x, 0f).normalized : Vector3.up;
                Vector3 position;
                float stream;
                if (batch.StagedFlow)
                {
                    var rise = Smooth01(Mathf.InverseLerp(0f, 0.34f, release));
                    stream = Smooth01(Mathf.InverseLerp(0.48f, 1f, release));
                    var lifted = Vector3.Lerp(particle.Start, particle.Lift, rise);
                    var topDrift = side * Mathf.Sin(release * 8.5f + particle.FlowPhase) * 0.035f * (1f - stream);
                    var upwardBreath = Vector3.up * Mathf.Sin(release * Mathf.PI) * 0.045f * (1f - stream);
                    var direct = Vector3.Lerp(particle.Lift, particle.Target, stream);
                    var streamWave = side * Mathf.Sin(stream * 8.2f + particle.FlowPhase) * 0.048f * Mathf.Sin(stream * Mathf.PI);
                    position = stream <= 0.001f
                        ? lifted + topDrift + upwardBreath
                        : direct + streamWave;
                }
                else
                {
                    var loosen = Smooth01(Mathf.InverseLerp(0f, 0.28f, release));
                    stream = Smooth01(Mathf.InverseLerp(0.18f, 1f, release));
                    var lifted = Vector3.Lerp(particle.Start, particle.Lift, loosen);
                    var line = Vector3.Lerp(lifted, particle.Target, stream);
                    var wave = Mathf.Sin(stream * 13.5f + particle.FlowPhase) * 0.095f * Mathf.Sin(stream * Mathf.PI);
                    var ribbon = Mathf.Sin(stream * Mathf.PI) * Mathf.Lerp(0.08f, 0.018f, stream);
                    position = line + side * (wave + ribbon);
                }

                var color = particle.Color;
                color.a = Mathf.Lerp(batch.StagedFlow ? 1f : 0.98f, 0.08f, Smooth01(Mathf.InverseLerp(0.88f, 1f, stream)));
                var scale = particle.Size * Mathf.Lerp(batch.StagedFlow ? 1.18f : 1.24f, particle.EndScale, stream);
                WriteParticleQuad(batch.Vertices, batch.Colors, index, position, scale, color);
            }

            batch.Mesh.vertices = batch.Vertices;
            batch.Mesh.colors = batch.Colors;
            batch.Mesh.RecalculateBounds();
        }

        private static void DestroyConvergeParticleBatch(ConvergeParticleBatch batch)
        {
            if (batch?.GameObject != null)
            {
                Destroy(batch.GameObject);
            }
        }

        private static void WriteParticleStaticData(int index, Vector2[] uvs, int[] triangles)
        {
            var vertexIndex = index * 4;
            var triangleIndex = index * 6;
            uvs[vertexIndex] = new Vector2(0f, 0f);
            uvs[vertexIndex + 1] = new Vector2(1f, 0f);
            uvs[vertexIndex + 2] = new Vector2(1f, 1f);
            uvs[vertexIndex + 3] = new Vector2(0f, 1f);
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        private static void WriteParticleQuad(Vector3[] vertices, Color[] colors, int index, Vector3 center, float scale, Color color)
        {
            var vertexIndex = index * 4;
            vertices[vertexIndex] = center + new Vector3(-scale, -scale, 0f);
            vertices[vertexIndex + 1] = center + new Vector3(scale, -scale, 0f);
            vertices[vertexIndex + 2] = center + new Vector3(scale, scale, 0f);
            vertices[vertexIndex + 3] = center + new Vector3(-scale, scale, 0f);
            colors[vertexIndex] = color;
            colors[vertexIndex + 1] = color;
            colors[vertexIndex + 2] = color;
            colors[vertexIndex + 3] = color;
        }

        private Vector3 GetRandomScreenLocalPosition()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                return new Vector3(UnityEngine.Random.Range(-8.5f, 8.5f), UnityEngine.Random.Range(-4.8f, 4.8f), 0f);
            }

            return ViewportToLocalWorldPoint(
                mainCamera,
                new Vector2(UnityEngine.Random.Range(0.04f, 0.96f), UnityEngine.Random.Range(0.08f, 0.94f)));
        }

        private Color SampleSpriteColor(Sprite sprite, Vector2 localOffset, Bounds bounds)
        {
            if (sprite == null || sprite.texture == null)
            {
                return Color.white;
            }

            var texture = GetReadableSpriteTexture(sprite);
            if (texture == null)
            {
                return Color.Lerp(new Color(0.86f, 0.82f, 0.7f, 1f), Color.white, UnityEngine.Random.Range(0.08f, 0.34f));
            }

            var rect = sprite.textureRect;
            var u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, localOffset.x);
            var v = Mathf.InverseLerp(bounds.min.y, bounds.max.y, localOffset.y);
            var x = Mathf.Clamp(Mathf.RoundToInt(rect.x + u * (rect.width - 1f)), 0, texture.width - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(rect.y + v * (rect.height - 1f)), 0, texture.height - 1);
            var color = texture.GetPixel(x, y);
            color.a = 1f;
            return color;
        }

        private Texture2D GetReadableSpriteTexture(Sprite sprite)
        {
            var texture = sprite.texture;
            if (texture == null)
            {
                return null;
            }

            if (texture.isReadable)
            {
                return texture;
            }

            if (readableSpriteCache.TryGetValue(sprite, out var readableTexture) && readableTexture != null)
            {
                return readableTexture;
            }

            readableTexture = CreateReadableTextureCopy(texture);
            if (readableTexture != null)
            {
                readableSpriteCache[sprite] = readableTexture;
            }

            return readableTexture;
        }

        private static Texture2D CreateReadableTextureCopy(Texture source)
        {
            if (source == null)
            {
                return null;
            }

            var previous = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;
                var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private void ClearReadableSpriteCache()
        {
            foreach (var texture in readableSpriteCache.Values)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            readableSpriteCache.Clear();
        }

        private static Sprite CreateParticleSprite()
        {
            const int size = 24;
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
                    var alpha = Mathf.Clamp01(1f - distance * 2.8f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateRingSprite()
        {
            const int size = 96;
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
                    var ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.78f) * 20f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, ring));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateSolidSprite(Color color)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite CreateMagicCircleSprite()
        {
            const int size = 512;
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
                    var point = new Vector2(x, y);
                    var offset = point - center;
                    var radius = offset.magnitude / (size * 0.5f);
                    var angle = Mathf.Atan2(offset.y, offset.x);
                    var outer = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.82f) * 70f);
                    var inner = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.52f) * 80f) * 0.7f;
                    var spokes = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin(angle * 6f)) * 34f) * Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(radius - 0.58f));
                    var alpha = Mathf.Clamp01(outer + inner + spokes * 0.42f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Material CreateParticleMaterial(Sprite sprite)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            if (sprite != null && sprite.texture != null)
            {
                material.mainTexture = sprite.texture;
            }

            return material;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("Event System");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
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
                    var color = fill;
                    var isBorder = x < 5 || x >= width - 5 || y < 5 || y >= height - 5;
                    if (isBorder)
                    {
                        color = Color.Lerp(line, Color.white, 0.18f);
                    }
                    else if (drawBack)
                    {
                        var dx = x - width * 0.5f;
                        var dy = y - height * 0.5f;
                        var distance = Mathf.Sqrt(dx * dx + dy * dy);
                        var centerRing = distance > 25f && distance < 30f;
                        var verticalRay = Mathf.Abs(dx) < 1.7f && Mathf.Abs(dy) < 88f;
                        var horizontalRay = Mathf.Abs(dy) < 1.7f && Mathf.Abs(dx) < 50f;
                        color = new Color(0.04f, 0.08f, 0.16f, 1f);
                        if (centerRing || verticalRay || horizontalRay)
                        {
                            color = line;
                        }
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 180f);
        }

        private readonly struct SpreadDraw
        {
            public SpreadDraw(TarotRuntimeCard card, TarotOrientation orientation, SpreadCardSlotDefinition slot)
            {
                Card = card;
                Orientation = orientation;
                Slot = slot;
            }

            public TarotRuntimeCard Card { get; }
            public TarotOrientation Orientation { get; }
            public SpreadCardSlotDefinition Slot { get; }
        }

        private readonly struct StagedDraw
        {
            public StagedDraw(CardDrawCardView view, SpreadDraw draw, int slotIndex)
            {
                View = view;
                Draw = draw;
                SlotIndex = slotIndex;
            }

            public CardDrawCardView View { get; }
            public SpreadDraw Draw { get; }
            public int SlotIndex { get; }
        }

        private readonly struct ConvergeParticle
        {
            public ConvergeParticle(
                Vector3 start,
                Vector3 lift,
                Vector3 control,
                Vector3 target,
                Color color,
                float size,
                float delay,
                float endScale,
                float flowPhase)
            {
                Start = start;
                Lift = lift;
                Control = control;
                Target = target;
                Color = color;
                Size = size;
                Delay = delay;
                EndScale = endScale;
                FlowPhase = flowPhase;
            }

            public Vector3 Start { get; }
            public Vector3 Lift { get; }
            public Vector3 Control { get; }
            public Vector3 Target { get; }
            public Color Color { get; }
            public float Size { get; }
            public float Delay { get; }
            public float EndScale { get; }
            public float FlowPhase { get; }
        }

        private sealed class ConvergeParticleBatch
        {
            public ConvergeParticleBatch(GameObject gameObject, Mesh mesh, ConvergeParticle[] particles, Vector3[] vertices, Color[] colors, bool stagedFlow)
            {
                GameObject = gameObject;
                Mesh = mesh;
                Particles = particles;
                Vertices = vertices;
                Colors = colors;
                StagedFlow = stagedFlow;
            }

            public GameObject GameObject { get; }
            public Mesh Mesh { get; }
            public ConvergeParticle[] Particles { get; }
            public Vector3[] Vertices { get; }
            public Color[] Colors { get; }
            public bool StagedFlow { get; }
        }

        private sealed class HoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private Action enter;
            private Action exit;

            public void Initialize(Action onEnter, Action onExit)
            {
                enter = onEnter;
                exit = onExit;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                enter?.Invoke();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                exit?.Invoke();
            }
        }

        private sealed class MagicCirclePulse : MonoBehaviour
        {
            private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();

            private void Awake()
            {
                renderers = GetComponentsInChildren<SpriteRenderer>();
            }

            private void Update()
            {
                transform.Rotate(0f, 0f, 3.5f * Time.deltaTime);
                var pulse = 0.82f + Mathf.Sin(Time.time * 1.4f) * 0.18f;
                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    var color = renderer.color;
                    color.a = 0.18f + pulse * 0.1f;
                    renderer.color = color;
                }
            }
        }
    }
}
