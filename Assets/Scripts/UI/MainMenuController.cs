using System;
using Tarot.Appearance;
using Tarot.SpreadReading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tarot.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float ColumnAnimationSpeed = 9f;
        private const int MainMenuCount = 6;
        private const int GalleryCategoryCount = 3;

        [SerializeField] private string title = "Tarot";
        [SerializeField] private Color textColor = new(0.9f, 0.9f, 0.88f, 1f);
        [SerializeField] private Color mutedTextColor = new(0.58f, 0.6f, 0.66f, 1f);

        private readonly System.Collections.Generic.IReadOnlyList<SpreadDefinition> spreadItems = SpreadDefinitionCatalog.All;

        private Font defaultFont;
        private RectTransform titleRect;
        private RectTransform subtitleRect;
        private RectTransform mainColumn;
        private RectTransform spreadColumn;
        private RectTransform galleryColumn;
        private RectTransform detailPanel;
        private RectTransform placeholderPanel;
        private RectTransform galleryPanel;
        private RectTransform galleryTrack;
        private RectTransform spreadPreviewRoot;
        private RectTransform firstSeparator;
        private RectTransform secondSeparator;
        private CanvasGroup spreadGroup;
        private CanvasGroup galleryCategoryGroup;
        private CanvasGroup detailGroup;
        private CanvasGroup placeholderGroup;
        private CanvasGroup galleryGroup;
        private CanvasGroup separatorGroup;
        private Text detailTitle;
        private Text detailMeta;
        private Text detailBody;
        private Text detailPositions;
        private Text placeholderTitle;
        private Text placeholderBody;
        private Text galleryTitle;
        private Text galleryBody;
        private Text galleryStatus;
        private Text galleryPlaceholder;
        private Button startSpreadButton;
        private Text startSpreadLabel;
        private MenuButton[] mainButtons;
        private MenuButton[] spreadButtons;
        private MenuButton[] galleryButtons;
        private GalleryCardView[] galleryCards = Array.Empty<GalleryCardView>();
        private Image[] spreadPreviewCards = Array.Empty<Image>();
        private Text[] spreadPreviewLabels = Array.Empty<Text>();
        private int selectedMainIndex = -1;
        private int selectedSpreadIndex;
        private int selectedGalleryIndex;
        private float galleryScroll;
        private bool isSpreadOpen;
        private bool isGalleryOpen;
        private bool isGalleryDragging;

        public event Action DailyReadingRequested;
        public event Action<SpreadDefinition> SpreadReadingRequested;

        private void Awake()
        {
            defaultFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Songti SC", "STSong", "Hiragino Mincho ProN", "PingFang SC", "Arial" },
                18);
            BuildMenu();
            SelectHome();
        }

        private void Update()
        {
            AnimateLayout();
        }

        private void BuildMenu()
        {
            EnsureEventSystem();
            var canvas = CreateCanvas();
            CreateTitle(canvas.transform);
            CreateSubtitle(canvas.transform);
            CreateMainColumn(canvas.transform);
            CreateSpreadColumn(canvas.transform);
            CreateGalleryColumn(canvas.transform);
            CreateDetailPanel(canvas.transform);
            CreatePlaceholderPanel(canvas.transform);
            CreateGalleryPanel(canvas.transform);
            CreateSeparators(canvas.transform);
        }

        private void SelectHome()
        {
            selectedMainIndex = -1;
            isSpreadOpen = false;
            isGalleryOpen = false;
            SetColumnVisible(spreadGroup, false);
            SetColumnVisible(galleryCategoryGroup, false);
            SetColumnVisible(detailGroup, false);
            SetColumnVisible(placeholderGroup, false);
            SetColumnVisible(galleryGroup, false);
            SetColumnVisible(separatorGroup, false);
            RefreshMainButtons();
        }

        private void SelectMain(int index)
        {
            selectedMainIndex = index;
            isSpreadOpen = index == 1;
            isGalleryOpen = index == 3;
            RefreshMainButtons();

            if (isSpreadOpen)
            {
                selectedSpreadIndex = Mathf.Clamp(selectedSpreadIndex, 0, spreadItems.Count - 1);
                SetColumnVisible(spreadGroup, true);
                SetColumnVisible(galleryCategoryGroup, false);
                SetColumnVisible(detailGroup, true);
                SetColumnVisible(placeholderGroup, false);
                SetColumnVisible(galleryGroup, false);
                SetColumnVisible(separatorGroup, true);
                ShowSpreadDetails(selectedSpreadIndex);
                RefreshSpreadButtons();
                return;
            }

            if (isGalleryOpen)
            {
                SetColumnVisible(spreadGroup, false);
                SetColumnVisible(galleryCategoryGroup, true);
                SetColumnVisible(detailGroup, false);
                SetColumnVisible(placeholderGroup, false);
                SetColumnVisible(galleryGroup, true);
                SetColumnVisible(separatorGroup, true);
                galleryScroll = CardBackGalleryCatalog.SelectedIndex;
                SelectGalleryCategory(selectedGalleryIndex);
                return;
            }

            SetColumnVisible(spreadGroup, false);
            SetColumnVisible(galleryCategoryGroup, false);
            SetColumnVisible(detailGroup, false);
            SetColumnVisible(placeholderGroup, true);
            SetColumnVisible(galleryGroup, false);
            SetColumnVisible(separatorGroup, true);
            ShowPlaceholder(index);
        }

        private void SelectSpread(int index)
        {
            selectedSpreadIndex = index;
            ShowSpreadDetails(index);
            RefreshSpreadButtons();
        }

        private void SelectGalleryCategory(int index)
        {
            selectedGalleryIndex = Mathf.Clamp(index, 0, GalleryCategoryCount - 1);
            RefreshGalleryButtons();

            var isCardBack = selectedGalleryIndex == 0;
            galleryTrack.gameObject.SetActive(isCardBack);
            galleryPlaceholder.gameObject.SetActive(!isCardBack);
            galleryStatus.gameObject.SetActive(isCardBack);

            if (isCardBack)
            {
                galleryScroll = CardBackGalleryCatalog.SelectedIndex;
                ShowGallerySelection(CardBackGalleryCatalog.SelectedIndex);
                UpdateGalleryShowcase(1f);
                return;
            }

            galleryTitle.text = GetGalleryCategoryLabel(selectedGalleryIndex);
            galleryBody.text = selectedGalleryIndex == 1
                ? "未来会在这里切换桌布材质与背景氛围。"
                : "未来会在这里切换牌面主题与插画风格。";
            galleryPlaceholder.text = selectedGalleryIndex == 1
                ? "桌布展台 即将开放"
                : "牌面展台 即将开放";
        }

        private void ShowSpreadDetails(int index)
        {
            var item = spreadItems[index];
            detailTitle.text = item.Title;
            detailMeta.text = $"{item.CardCount} 张牌";
            detailBody.text = $"{item.Summary}\n{item.Description}";
            detailPositions.text = $"牌位：{FormatSlotNames(item)}";
            startSpreadButton.gameObject.SetActive(true);
            startSpreadButton.interactable = true;
            startSpreadLabel.text = "开始抽牌";
            UpdateSpreadPreview(item);
        }

        private void ShowPlaceholder(int index)
        {
            var label = GetMainMenuLabel(index);
            placeholderTitle.text = label;
            placeholderBody.text = index switch
            {
                2 => "占卜日记会在这里展开历史记录、收藏与复盘。",
                3 => "展柜会在这里管理卡背、桌布和牌面外观。",
                4 => "设置会在这里整理音效、背景和互动偏好。",
                _ => "这个功能还没有开放。"
            };
        }

        private void AnimateLayout()
        {
            var targetMainX = selectedMainIndex < 0 ? 0f : -590f;
            var targetSpreadX = isSpreadOpen ? -350f : -128f;
            var targetGalleryColumnX = isGalleryOpen ? -350f : -128f;
            var targetDetailX = isSpreadOpen ? -84f : 610f;
            var targetPlaceholderX = selectedMainIndex >= 0 && !isSpreadOpen ? -10f : 360f;
            var targetGalleryX = isGalleryOpen ? -18f : 760f;
            var t = 1f - Mathf.Exp(-ColumnAnimationSpeed * Time.unscaledDeltaTime);

            mainColumn.anchoredPosition = Vector2.Lerp(mainColumn.anchoredPosition, new Vector2(targetMainX, 50f), t);
            spreadColumn.anchoredPosition = Vector2.Lerp(spreadColumn.anchoredPosition, new Vector2(targetSpreadX, 44f), t);
            galleryColumn.anchoredPosition = Vector2.Lerp(galleryColumn.anchoredPosition, new Vector2(targetGalleryColumnX, 44f), t);
            detailPanel.anchoredPosition = Vector2.Lerp(detailPanel.anchoredPosition, new Vector2(targetDetailX, 44f), t);
            placeholderPanel.anchoredPosition = Vector2.Lerp(placeholderPanel.anchoredPosition, new Vector2(targetPlaceholderX, 24f), t);
            galleryPanel.anchoredPosition = Vector2.Lerp(galleryPanel.anchoredPosition, new Vector2(targetGalleryX, 44f), t);
            firstSeparator.anchoredPosition = Vector2.Lerp(firstSeparator.anchoredPosition, new Vector2(-585f, -26f), t);
            secondSeparator.anchoredPosition = Vector2.Lerp(secondSeparator.anchoredPosition, new Vector2(-378f, -26f), t);
            UpdateGalleryShowcase(t);
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Main Menu Canvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void CreateTitle(Transform parent)
        {
            var titleText = CreateText("Title", parent, title, 124, FontStyle.Normal, textColor, TextAnchor.MiddleCenter);
            titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 354f);
            titleRect.sizeDelta = new Vector2(720f, 142f);
        }

        private void CreateSubtitle(Transform parent)
        {
            var subtitle = CreateText("Subtitle", parent, "在静默星光中，选择今天的问题。", 24, FontStyle.Normal, mutedTextColor, TextAnchor.MiddleCenter);
            subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(0f, 265f);
            subtitleRect.sizeDelta = new Vector2(760f, 48f);
        }

        private void CreateMainColumn(Transform parent)
        {
            mainColumn = CreateColumn(parent, "Main Navigation", new Vector2(0f, 30f), new Vector2(390f, 380f));
            mainButtons = new MenuButton[MainMenuCount];

            const float buttonHeight = 58f;
            const float gap = 18f;
            const float startY = 120f;
            for (var index = 0; index < MainMenuCount; index++)
            {
                var captured = index;
                var button = CreateNavButton(
                    mainColumn,
                    GetMainMenuLabel(index),
                    new Vector2(0f, startY - index * (buttonHeight + gap)),
                    new Vector2(360f, buttonHeight),
                    () => HandleMainMenuClick(captured),
                    true);
                mainButtons[index] = button;
            }
        }

        private void CreateSpreadColumn(Transform parent)
        {
            spreadColumn = CreateColumn(parent, "Spread Navigation", new Vector2(-350f, 24f), new Vector2(430f, 330f));
            spreadGroup = spreadColumn.gameObject.AddComponent<CanvasGroup>();

            spreadButtons = new MenuButton[spreadItems.Count];
            for (var index = 0; index < spreadItems.Count; index++)
            {
                var item = spreadItems[index];
                var captured = index;
                var button = CreateNavButton(
                    spreadColumn,
                    item.Title,
                    new Vector2(-24f, 126f - index * 50f),
                    new Vector2(390f, 46f),
                    () => SelectSpread(captured),
                    true);

                var hover = button.Button.gameObject.AddComponent<HoverTarget>();
                hover.Initialize(() => ShowSpreadDetails(captured), () => ShowSpreadDetails(selectedSpreadIndex));
                spreadButtons[index] = button;
            }
        }

        private void CreateGalleryColumn(Transform parent)
        {
            galleryColumn = CreateColumn(parent, "Gallery Navigation", new Vector2(-350f, 24f), new Vector2(430f, 220f));
            galleryCategoryGroup = galleryColumn.gameObject.AddComponent<CanvasGroup>();
            galleryButtons = new MenuButton[GalleryCategoryCount];

            for (var index = 0; index < GalleryCategoryCount; index++)
            {
                var captured = index;
                var button = CreateNavButton(
                    galleryColumn,
                    GetGalleryCategoryLabel(index),
                    new Vector2(0f, 126f - index * 66f),
                    new Vector2(390f, 54f),
                    () => SelectGalleryCategory(captured),
                    true);
                galleryButtons[index] = button;
            }
        }

        private void CreateDetailPanel(Transform parent)
        {
            detailPanel = CreateColumn(parent, "Spread Detail Text", new Vector2(10f, 24f), new Vector2(880f, 520f));
            detailGroup = detailPanel.gameObject.AddComponent<CanvasGroup>();

            detailTitle = CreateText("Detail Title", detailPanel, string.Empty, 34, FontStyle.Bold, new Color(0.94f, 0.94f, 0.88f, 1f), TextAnchor.UpperLeft);
            detailTitle.rectTransform.anchoredPosition = new Vector2(130f, 126f);
            detailTitle.rectTransform.sizeDelta = new Vector2(738f, 44f);

            detailMeta = CreateText("Detail Meta", detailPanel, string.Empty, 20, FontStyle.Normal, new Color(0.66f, 0.7f, 0.72f, 0.96f), TextAnchor.UpperLeft);
            detailMeta.rectTransform.anchoredPosition = new Vector2(130f, 86f);
            detailMeta.rectTransform.sizeDelta = new Vector2(738f, 30f);

            detailBody = CreateText("Detail Body", detailPanel, string.Empty, 22, FontStyle.Normal, new Color(0.84f, 0.86f, 0.82f, 0.98f), TextAnchor.UpperLeft);
            detailBody.rectTransform.anchoredPosition = new Vector2(130f, 34f);
            detailBody.rectTransform.sizeDelta = new Vector2(738f, 88f);

            CreatePanelLine(detailPanel, "Detail Divider", new Vector2(130f, 0f), 650f, new Color(0.72f, 0.55f, 0.26f, 0.52f));

            detailPositions = CreateText("Detail Positions", detailPanel, string.Empty, 19, FontStyle.Normal, new Color(0.86f, 0.74f, 0.45f, 0.98f), TextAnchor.UpperLeft);
            detailPositions.rectTransform.anchoredPosition = new Vector2(130f, -26f);
            detailPositions.rectTransform.sizeDelta = new Vector2(738f, 36f);

            spreadPreviewRoot = CreateColumn(detailPanel, "Spread Preview", new Vector2(130f, -78f), new Vector2(720f, 178f));
            spreadPreviewCards = new Image[7];
            spreadPreviewLabels = new Text[7];
            for (var index = 0; index < spreadPreviewCards.Length; index++)
            {
                spreadPreviewCards[index] = CreatePreviewCard(spreadPreviewRoot, $"Spread Preview Card {index}", index + 1, out spreadPreviewLabels[index]);
            }

            startSpreadButton = CreateTextButton(detailPanel, "开始抽牌", new Vector2(130f, -336f), new Vector2(188f, 46f), () => SpreadReadingRequested?.Invoke(spreadItems[selectedSpreadIndex]));
            startSpreadLabel = startSpreadButton.GetComponentInChildren<Text>();
        }

        private void CreatePlaceholderPanel(Transform parent)
        {
            placeholderPanel = CreateColumn(parent, "Placeholder Detail Text", new Vector2(-10f, 24f), new Vector2(560f, 224f));
            placeholderGroup = placeholderPanel.gameObject.AddComponent<CanvasGroup>();

            placeholderTitle = CreateText("Placeholder Title", placeholderPanel, string.Empty, 34, FontStyle.Bold, new Color(0.94f, 0.94f, 0.88f, 1f), TextAnchor.UpperLeft);
            placeholderTitle.rectTransform.anchoredPosition = new Vector2(0f, 62f);
            placeholderTitle.rectTransform.sizeDelta = new Vector2(482f, 46f);

            placeholderBody = CreateText("Placeholder Body", placeholderPanel, string.Empty, 22, FontStyle.Normal, new Color(0.84f, 0.86f, 0.82f, 0.98f), TextAnchor.UpperLeft);
            placeholderBody.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            placeholderBody.rectTransform.sizeDelta = new Vector2(482f, 86f);
        }

        private void CreateGalleryPanel(Transform parent)
        {
            galleryPanel = CreateColumn(parent, "Card Gallery Showcase", new Vector2(760f, 24f), new Vector2(900f, 390f));
            galleryGroup = galleryPanel.gameObject.AddComponent<CanvasGroup>();

            var dragImage = galleryPanel.gameObject.AddComponent<Image>();
            dragImage.color = Color.clear;
            dragImage.raycastTarget = true;

            var dragSurface = galleryPanel.gameObject.AddComponent<GalleryDragSurface>();
            dragSurface.Initialize(HandleGalleryDragBegin, HandleGalleryDrag, HandleGalleryDragEnd);

            galleryTitle = CreateText("Gallery Title", galleryPanel, "卡背图案", 34, FontStyle.Bold, new Color(0.94f, 0.94f, 0.88f, 1f), TextAnchor.UpperLeft);
            galleryTitle.rectTransform.anchoredPosition = new Vector2(90f, 126f);
            galleryTitle.rectTransform.sizeDelta = new Vector2(740f, 44f);

            galleryBody = CreateText("Gallery Body", galleryPanel, string.Empty, 21, FontStyle.Normal, new Color(0.84f, 0.86f, 0.82f, 0.98f), TextAnchor.UpperLeft);
            galleryBody.rectTransform.anchoredPosition = new Vector2(90f, 84f);
            galleryBody.rectTransform.sizeDelta = new Vector2(740f, 62f);

            galleryTrack = CreateColumn(galleryPanel, "Card Back Showcase Track", new Vector2(90f, -52f), new Vector2(860f, 222f));
            galleryTrack.gameObject.AddComponent<RectMask2D>();
            CreatePanelLine(galleryPanel, "Gallery Pedestal", new Vector2(90f, -171f), 680f, new Color(0.72f, 0.55f, 0.26f, 0.52f));

            galleryPlaceholder = CreateText("Gallery Placeholder", galleryPanel, string.Empty, 24, FontStyle.Normal, new Color(0.72f, 0.74f, 0.7f, 0.92f), TextAnchor.MiddleCenter);
            galleryPlaceholder.rectTransform.anchoredPosition = new Vector2(90f, -54f);
            galleryPlaceholder.rectTransform.sizeDelta = new Vector2(740f, 136f);

            galleryStatus = CreateText("Gallery Status", galleryPanel, string.Empty, 18, FontStyle.Normal, new Color(0.86f, 0.74f, 0.45f, 0.98f), TextAnchor.MiddleCenter);
            galleryStatus.rectTransform.anchoredPosition = new Vector2(90f, -202f);
            galleryStatus.rectTransform.sizeDelta = new Vector2(740f, 36f);

            var items = CardBackGalleryCatalog.GetItems();
            galleryCards = new GalleryCardView[items.Length];
            for (var index = 0; index < items.Length; index++)
            {
                galleryCards[index] = CreateGalleryCard(galleryTrack, items[index]);
            }

            galleryScroll = CardBackGalleryCatalog.SelectedIndex;
            ShowGallerySelection(CardBackGalleryCatalog.SelectedIndex);
            UpdateGalleryShowcase(1f);
        }

        private GalleryCardView CreateGalleryCard(Transform parent, CardBackGalleryItem item)
        {
            var rootObject = new GameObject(item.Title);
            rootObject.transform.SetParent(parent, false);
            var root = rootObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(132f, 218f);

            var group = rootObject.AddComponent<CanvasGroup>();

            var shadowObject = new GameObject("Card Shadow");
            shadowObject.transform.SetParent(root, false);
            var shadow = shadowObject.AddComponent<Image>();
            shadow.color = new Color(0f, 0f, 0f, 0.36f);
            shadow.raycastTarget = false;
            shadow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            shadow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            shadow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            shadow.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            shadow.rectTransform.sizeDelta = new Vector2(104f, 178f);

            var imageObject = new GameObject("Card Back Preview");
            imageObject.transform.SetParent(root, false);
            var preview = imageObject.AddComponent<Image>();
            preview.sprite = item.Sprite;
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            preview.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            preview.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            preview.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            preview.rectTransform.anchoredPosition = Vector2.zero;
            preview.rectTransform.sizeDelta = new Vector2(104f, 178f);

            var label = CreateText("Card Back Label", root, item.Title, 17, FontStyle.Normal, new Color(0.9f, 0.88f, 0.8f, 1f), TextAnchor.MiddleCenter);
            label.rectTransform.anchoredPosition = new Vector2(0f, -108f);
            label.rectTransform.sizeDelta = new Vector2(160f, 32f);

            return new GalleryCardView(root, group, preview, label);
        }

        private Image CreatePreviewCard(Transform parent, string name, int number, out Text numberLabel)
        {
            var cardObject = new GameObject(name);
            cardObject.transform.SetParent(parent, false);
            var image = cardObject.AddComponent<Image>();
            image.color = new Color(0.86f, 0.74f, 0.45f, 0.17f);
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.sizeDelta = new Vector2(48f, 82f);
            numberLabel = CreateText("Preview Number", cardObject.transform, number.ToString(), 14, FontStyle.Bold, new Color(0.92f, 0.82f, 0.5f, 0.78f), TextAnchor.MiddleCenter);
            numberLabel.raycastTarget = false;
            numberLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            numberLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            numberLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            numberLabel.rectTransform.anchoredPosition = Vector2.zero;
            numberLabel.rectTransform.sizeDelta = new Vector2(28f, 22f);
            return image;
        }

        private void UpdateSpreadPreview(SpreadDefinition definition)
        {
            var layoutOffset = Vector2.zero;
            if (definition != null && definition.CardCount > 0)
            {
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                for (var index = 0; index < definition.CardCount; index++)
                {
                    var position = GetPreviewPosition(definition.Slots[index]);
                    min = Vector2.Min(min, position);
                    max = Vector2.Max(max, position);
                }

                layoutOffset.x = -(min.x + max.x) * 0.5f;
                if (definition.RevealFlow == SpreadRevealFlow.StagedReveal && definition.Id != "time-flow")
                {
                    const float targetBottom = -220f;
                    layoutOffset.y = targetBottom - (min.y - 41f);
                }
            }

            for (var index = 0; index < spreadPreviewCards.Length; index++)
            {
                var card = spreadPreviewCards[index];
                var label = spreadPreviewLabels[index];
                var isVisible = definition != null && index < definition.CardCount;
                card.gameObject.SetActive(isVisible);
                if (!isVisible)
                {
                    continue;
                }

                var slot = definition.Slots[index];
                card.rectTransform.anchoredPosition = GetPreviewPosition(slot) + layoutOffset;
                card.rectTransform.localRotation = Quaternion.identity;
                card.color = new Color(0.86f, 0.74f, 0.45f, 0.2f);
                if (label != null)
                {
                    label.text = (index + 1).ToString();
                }
            }
        }

        private static Vector2 GetPreviewPosition(SpreadCardSlotDefinition slot)
        {
            return new Vector2(slot.PreviewPosition.x * 1.42f, slot.PreviewPosition.y);
        }

        private static string FormatSlotNames(SpreadDefinition definition)
        {
            if (definition == null || definition.CardCount == 0)
            {
                return string.Empty;
            }

            var names = string.Empty;
            for (var index = 0; index < definition.CardCount; index++)
            {
                if (index > 0)
                {
                    names += " / ";
                }

                names += definition.Slots[index].Name;
            }

            return names;
        }

        private void CreateSeparators(Transform parent)
        {
            var groupObject = new GameObject("Navigation Separators");
            groupObject.transform.SetParent(parent, false);
            separatorGroup = groupObject.AddComponent<CanvasGroup>();

            firstSeparator = CreateVerticalLine(groupObject.transform, "Main Separator", new Vector2(-585f, -46f), 440f);
            secondSeparator = CreateVerticalLine(groupObject.transform, "Detail Separator", new Vector2(-378f, -46f), 440f);
        }

        private RectTransform CreateColumn(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var columnObject = new GameObject(name);
            columnObject.transform.SetParent(parent, false);

            var rect = columnObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreatePanelLine(Transform parent, string name, Vector2 position, float width, Color color)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            var image = lineObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = new Vector2(width, 1.4f);
            return image;
        }

        private static RectTransform CreateVerticalLine(Transform parent, string name, Vector2 position, float height)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            var image = lineObject.AddComponent<Image>();
            image.color = new Color(0.72f, 0.55f, 0.26f, 0.58f);
            image.raycastTarget = false;

            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(2.2f, height);
            return rect;
        }

        private MenuButton CreateNavButton(RectTransform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool isEnabled)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = Color.clear;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = isEnabled;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.045f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.075f);
            colors.selectedColor = Color.clear;
            colors.disabledColor = Color.clear;
            colors.fadeDuration = 0.18f;
            button.colors = colors;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var pointer = CreatePointer(buttonObject.transform, "Pointer", new Vector2(9f, 0f));

            var labelText = CreateText("Label", buttonObject.transform, label, 24, FontStyle.Normal, isEnabled ? textColor : new Color(0.5f, 0.52f, 0.56f, 0.92f), TextAnchor.MiddleLeft);
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(28f, 0f);
            labelRect.offsetMax = new Vector2(-16f, 0f);

            return new MenuButton(button, image, pointer, labelText);
        }

        private Button CreateTextButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = CreateNavButton(parent.GetComponent<RectTransform>(), label, position, size, action, true);
            button.Label.alignment = TextAnchor.MiddleCenter;
            button.Label.color = new Color(0.86f, 0.74f, 0.45f, 1f);
            button.Label.rectTransform.offsetMin = Vector2.zero;
            button.Label.rectTransform.offsetMax = Vector2.zero;
            return button.Button;
        }

        private static CanvasGroup CreatePointer(Transform parent, string name, Vector2 position)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(30f, 28f);

            var shadowObject = new GameObject("Pointer Shadow");
            shadowObject.transform.SetParent(root.transform, false);
            var shadow = shadowObject.AddComponent<Image>();
            shadow.sprite = CreateTriangleSprite(new Color(0f, 0f, 0f, 0.54f));
            shadow.raycastTarget = false;
            shadow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            shadow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            shadow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            shadow.rectTransform.anchoredPosition = new Vector2(2.4f, -2.2f);
            shadow.rectTransform.sizeDelta = new Vector2(24f, 22f);

            var faceObject = new GameObject("Pointer Face");
            faceObject.transform.SetParent(root.transform, false);
            var face = faceObject.AddComponent<Image>();
            face.sprite = CreateTriangleSprite(new Color(0.9f, 0.7f, 0.3f, 1f));
            face.raycastTarget = false;
            face.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            face.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            face.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            face.rectTransform.anchoredPosition = Vector2.zero;
            face.rectTransform.sizeDelta = new Vector2(24f, 22f);

            var highlightObject = new GameObject("Pointer Highlight");
            highlightObject.transform.SetParent(root.transform, false);
            var highlight = highlightObject.AddComponent<Image>();
            highlight.color = new Color(1f, 0.92f, 0.58f, 0.82f);
            highlight.raycastTarget = false;
            highlight.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            highlight.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            highlight.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            highlight.rectTransform.anchoredPosition = new Vector2(-3f, 4f);
            highlight.rectTransform.sizeDelta = new Vector2(7f, 2f);

            return group;
        }

        private Text CreateText(string objectName, Transform parent, string value, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            var textObject = new GameObject(objectName);
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

        private void RefreshMainButtons()
        {
            for (var index = 0; index < mainButtons.Length; index++)
            {
                SetButtonSelected(mainButtons[index], index == selectedMainIndex);
            }
        }

        private void RefreshSpreadButtons()
        {
            for (var index = 0; index < spreadButtons.Length; index++)
            {
                SetButtonSelected(spreadButtons[index], index == selectedSpreadIndex);
            }
        }

        private void RefreshGalleryButtons()
        {
            for (var index = 0; index < galleryButtons.Length; index++)
            {
                SetButtonSelected(galleryButtons[index], index == selectedGalleryIndex);
            }
        }

        private void SetButtonSelected(MenuButton button, bool isSelected)
        {
            button.Image.color = Color.clear;
            button.Pointer.alpha = isSelected ? 1f : 0f;
            button.Label.color = isSelected ? new Color(0.95f, 0.92f, 0.82f, 1f) : textColor;
        }

        private static void SetColumnVisible(CanvasGroup group, bool isVisible)
        {
            group.alpha = isVisible ? 1f : 0f;
            group.interactable = isVisible;
            group.blocksRaycasts = isVisible;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void HandleGalleryDragBegin()
        {
            if (!isGalleryOpen)
            {
                return;
            }

            isGalleryDragging = true;
        }

        private void HandleGalleryDrag(Vector2 delta)
        {
            if (!isGalleryOpen || galleryCards.Length == 0)
            {
                return;
            }

            galleryScroll = Mathf.Clamp(galleryScroll - delta.x / 172f, 0f, galleryCards.Length - 1f);
            ShowGallerySelection(Mathf.RoundToInt(galleryScroll));
        }

        private void HandleGalleryDragEnd()
        {
            if (!isGalleryOpen || galleryCards.Length == 0)
            {
                isGalleryDragging = false;
                return;
            }

            isGalleryDragging = false;
            var selectedIndex = Mathf.Clamp(Mathf.RoundToInt(galleryScroll), 0, galleryCards.Length - 1);
            CardBackGalleryCatalog.SelectedIndex = selectedIndex;
            ShowGallerySelection(selectedIndex);
        }

        private void ShowGallerySelection(int index)
        {
            if (galleryTitle == null || galleryBody == null || galleryStatus == null)
            {
                return;
            }

            var item = CardBackGalleryCatalog.GetItem(index);
            galleryTitle.text = item.Title;
            galleryBody.text = item.Description;
            galleryStatus.text = index == CardBackGalleryCatalog.SelectedIndex
                ? "使用中 · 拖拽展柜切换卡背"
                : "松开后应用这个卡背";
        }

        private void UpdateGalleryShowcase(float smoothing)
        {
            if (galleryCards == null || galleryCards.Length == 0)
            {
                return;
            }

            if (!isGalleryDragging)
            {
                galleryScroll = Mathf.Lerp(galleryScroll, CardBackGalleryCatalog.SelectedIndex, smoothing);
            }

            const float spacing = 210f;
            for (var index = 0; index < galleryCards.Length; index++)
            {
                var card = galleryCards[index];
                var offset = index - galleryScroll;
                var distance = Mathf.Abs(offset);
                var focus = Mathf.Clamp01(1f - distance / 2.65f);
                var scale = Mathf.Lerp(0.72f, 1.16f, Smooth01(focus));
                var alpha = Mathf.Lerp(0.22f, 1f, focus);

                card.Root.anchoredPosition = new Vector2(offset * spacing, -distance * 10f);
                card.Root.localScale = Vector3.one * scale;
                card.Group.alpha = alpha;
                card.Preview.color = Color.Lerp(new Color(0.62f, 0.64f, 0.68f, 1f), Color.white, focus);
                card.Label.color = index == CardBackGalleryCatalog.SelectedIndex
                    ? new Color(0.95f, 0.78f, 0.42f, alpha)
                    : new Color(0.86f, 0.86f, 0.82f, alpha);
            }
        }

        private static string GetMainMenuLabel(int index)
        {
            return index switch
            {
                0 => "每日运势",
                1 => "牌阵占卜",
                2 => "占卜日记",
                3 => "展柜",
                4 => "设置",
                5 => "退出",
                _ => string.Empty
            };
        }

        private static string GetGalleryCategoryLabel(int index)
        {
            return index switch
            {
                0 => "卡背",
                1 => "桌布",
                2 => "牌面",
                _ => string.Empty
            };
        }

        private void HandleMainMenuClick(int index)
        {
            if (index == 0)
            {
                SelectHome();
                DailyReadingRequested?.Invoke();
                return;
            }

            if (index == 5)
            {
                Application.Quit();
                return;
            }

            SelectMain(index);
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

        private static Sprite CreateTriangleSprite(Color color)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var normalizedX = x / (float)(size - 1);
                    var normalizedY = Mathf.Abs(y / (float)(size - 1) - 0.5f) * 2f;
                    var inside = normalizedX <= 0.92f && normalizedY <= 1f - normalizedX / 0.92f;
                    var pixel = inside ? color : Color.clear;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private readonly struct MenuButton
        {
            public MenuButton(Button button, Image image, CanvasGroup pointer, Text label)
            {
                Button = button;
                Image = image;
                Pointer = pointer;
                Label = label;
            }

            public Button Button { get; }
            public Image Image { get; }
            public CanvasGroup Pointer { get; }
            public Text Label { get; }
        }

        private readonly struct GalleryCardView
        {
            public GalleryCardView(RectTransform root, CanvasGroup group, Image preview, Text label)
            {
                Root = root;
                Group = group;
                Preview = preview;
                Label = label;
            }

            public RectTransform Root { get; }
            public CanvasGroup Group { get; }
            public Image Preview { get; }
            public Text Label { get; }
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

        private sealed class GalleryDragSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private Action begin;
            private Action<Vector2> drag;
            private Action end;

            public void Initialize(Action onBegin, Action<Vector2> onDrag, Action onEnd)
            {
                begin = onBegin;
                drag = onDrag;
                end = onEnd;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                begin?.Invoke();
            }

            public void OnDrag(PointerEventData eventData)
            {
                drag?.Invoke(eventData.delta);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                end?.Invoke();
            }
        }
    }
}
