using System;
using UnityEngine;

namespace Tarot.Input
{
    public sealed class CameraGestureInputSource : MonoBehaviour
    {
        private const float CalibrationDuration = 1f;
        private const float GestureWarmupDuration = 0.35f;
        private const float SelectionCooldown = 1.1f;
        private const float PinchConfirmDuration = 0.14f;
        private const float HoverGraceDuration = 1.15f;
        private const float ConfirmFeedbackDuration = 0.28f;
        private const float HoldConfirmDuration = 0.85f;
        private const float CenterDeadZone = 0.08f;
        private const float CalibrationHorizontalTolerance = 0.12f;
        private const float MaxRotationDegreesPerFrame = 0.82f;
        private const float OpenHandDropoutGraceDuration = 1.15f;
        private const float EdgeOpenHandDropoutGraceDuration = 2.6f;
        private const float EdgeRotationAnchorInset = 0.035f;
        private const float MaxPalmJumpPerFrame = 0.28f;
        private const float EdgeInstabilityMargin = 0.075f;

        [SerializeField] private bool showDebugOverlay = true;

        private readonly HandGestureRecognizer recognizer = new();
        private IHandLandmarkProvider landmarkProvider;
        private Texture2D feedbackCircleTexture;
        private Texture2D feedbackRingTexture;
        private Texture2D onboardingPanelTexture;
        private HandGestureState currentState;
        private Vector2 smoothedPalm = new(0.5f, 0.5f);
        private Vector2 smoothedGestureCursor = new(0.5f, 0.5f);
        private float gestureWarmupTimer;
        private float calibrationTimer;
        private float holdTimer;
        private float pinchTimer;
        private float hoverGraceTimer;
        private float openHandDropoutTimer;
        private float confirmFeedbackTimer;
        private float selectionCooldownTimer;
        private float calibratedCenterX = 0.5f;
        private float smoothedRotation;
        private Vector2 lastOpenHandPalm = new(0.5f, 0.5f);
        private bool gestureEnabled;
        private bool calibrationVisible;
        private bool calibrated;
        private bool hasHand;
        private bool gestureHoverActive;
        private string statusMessage = string.Empty;
        private float statusMessageTimer;

        public event Action<float> RotationRequested;
        public event Action<Vector2> GestureHoverMoved;
        public event Action GestureHoverCleared;
        public event Action GestureConfirmRequested;
        public event Action<bool> EnableStateChanged;
        public event Func<Vector2, bool> HoldConfirmRequested;

        public bool IsGestureEnabled => gestureEnabled;

        private void OnDisable()
        {
            StopGestureTracking();
        }

        private void Update()
        {
            if (statusMessageTimer > 0f)
            {
                statusMessageTimer = Mathf.Max(0f, statusMessageTimer - Time.unscaledDeltaTime);
            }

            if (!gestureEnabled)
            {
                return;
            }

            gestureWarmupTimer = Mathf.Max(0f, gestureWarmupTimer - Time.deltaTime);
            confirmFeedbackTimer = Mathf.Max(0f, confirmFeedbackTimer - Time.deltaTime);
            selectionCooldownTimer = Mathf.Max(0f, selectionCooldownTimer - Time.deltaTime);

            if (landmarkProvider == null || !landmarkProvider.TryGetLatestFrame(out var frame))
            {
                hasHand = false;
                holdTimer = 0f;
                pinchTimer = 0f;
                DecayGestureHoverGrace();
                ClearGestureHoverIfExpired();
                if (!TryContinueOpenHandRotationDuringDropout())
                {
                    smoothedRotation = Mathf.Lerp(smoothedRotation, 0f, 1f - Mathf.Exp(-8f * Time.deltaTime));
                }

                return;
            }

            currentState = recognizer.Analyze(frame);
            hasHand = currentState.HasHand;
            if (!hasHand)
            {
                DecayGestureHoverGrace();
                ClearGestureHoverIfExpired();
                if (!TryContinueOpenHandRotationDuringDropout())
                {
                    smoothedRotation = Mathf.Lerp(smoothedRotation, 0f, 1f - Mathf.Exp(-8f * Time.deltaTime));
                }

                return;
            }

            var palmTarget = MirrorForPlayer(currentState.PalmCenter);
            var unstablePalmFrame = IsUnstablePalmFrame(palmTarget);
            if (!unstablePalmFrame)
            {
                smoothedPalm = Vector2.Lerp(smoothedPalm, palmTarget, 0.34f);
            }

            if (currentState.IsIndexPoint)
            {
                var pointerTarget = MirrorForPlayer(currentState.PointerCenter);
                smoothedGestureCursor = Vector2.Lerp(smoothedGestureCursor, pointerTarget, 0.42f);
            }
            else if (currentState.IsThreeFingerPinch)
            {
                var pinchTarget = MirrorForPlayer(currentState.PinchCenter);
                smoothedGestureCursor = Vector2.Lerp(smoothedGestureCursor, pinchTarget, 0.56f);
            }

            if (calibrationVisible || !calibrated)
            {
                UpdateCalibration();
                return;
            }

            if (gestureWarmupTimer > 0f)
            {
                return;
            }

            UpdateRotation(palmTarget, unstablePalmFrame);
            UpdateGestureHover();
            UpdatePinchSelection();
            UpdateUiHoldConfirm();
        }

        public bool SetGestureEnabled(bool enabled)
        {
            if (!enabled)
            {
                StopGestureTracking();
                EnableStateChanged?.Invoke(false);
                return false;
            }

            if (gestureEnabled)
            {
                return true;
            }

            landmarkProvider = ResolveProvider();
            if (landmarkProvider == null || !landmarkProvider.IsAvailable || !landmarkProvider.StartTracking())
            {
                statusMessage = landmarkProvider != null
                    ? landmarkProvider.StatusMessage
                    : "未找到 MediaPipe 手势识别组件";
                statusMessageTimer = 4f;
                StopGestureTracking();
                EnableStateChanged?.Invoke(false);
                return false;
            }

            gestureEnabled = true;
            calibrated = false;
            calibrationVisible = true;
            gestureWarmupTimer = GestureWarmupDuration;
            ResetTimers();
            EnableStateChanged?.Invoke(true);
            return true;
        }

        public void ShowOnboarding()
        {
            if (!gestureEnabled)
            {
                return;
            }

            calibrated = false;
            calibrationVisible = true;
            ClearGestureHoverIfNeeded();
            ResetTimers();
        }

        private IHandLandmarkProvider ResolveProvider()
        {
            var behaviours = GetComponents<MonoBehaviour>();
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IHandLandmarkProvider provider)
                {
                    return provider;
                }
            }

            return gameObject.AddComponent<MediaPipeHandLandmarkProvider>();
        }

        private void StopGestureTracking()
        {
            gestureEnabled = false;
            calibrated = false;
            calibrationVisible = false;
            hasHand = false;
            ClearGestureHoverIfNeeded();
            landmarkProvider?.StopTracking();
            landmarkProvider = null;
            ResetTimers();
            smoothedRotation = 0f;
        }

        private void UpdateCalibration()
        {
            var onYAxis =
                currentState.IsOpenHand &&
                Mathf.Abs(smoothedPalm.x - 0.5f) <= CalibrationHorizontalTolerance &&
                smoothedPalm.y > 0.25f &&
                smoothedPalm.y < 0.78f;
            calibrationTimer = onYAxis
                ? Mathf.Min(CalibrationDuration, calibrationTimer + Time.deltaTime)
                : Mathf.Max(0f, calibrationTimer - Time.deltaTime * 1.8f);

            if (calibrationTimer < CalibrationDuration)
            {
                return;
            }

            calibratedCenterX = smoothedPalm.x;
            calibrated = true;
            calibrationVisible = false;
            gestureWarmupTimer = GestureWarmupDuration;
            ResetTimers();
        }

        private void UpdateRotation(Vector2 palmTarget, bool unstablePalmFrame)
        {
            if (currentState.IsOpenHand && !currentState.IsIndexPoint && !currentState.IsThreeFingerPinch && !unstablePalmFrame)
            {
                lastOpenHandPalm = smoothedPalm;
                openHandDropoutTimer = OpenHandDropoutGraceDuration;
                ApplyRotationFromPalm(smoothedPalm);
                return;
            }

            if (currentState.IsOpenHand && !currentState.IsIndexPoint && !currentState.IsThreeFingerPinch && IsNearHorizontalEdge(palmTarget))
            {
                lastOpenHandPalm = GetEdgeRotationPalm(palmTarget);
                openHandDropoutTimer = EdgeOpenHandDropoutGraceDuration;
                ApplyRotationFromPalm(lastOpenHandPalm);
                return;
            }

            if (!currentState.IsIndexPoint && !currentState.IsThreeFingerPinch && TryContinueOpenHandRotationDuringDropout())
            {
                return;
            }

            openHandDropoutTimer = 0f;
            smoothedRotation = Mathf.Lerp(smoothedRotation, 0f, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        private bool IsUnstablePalmFrame(Vector2 palmTarget)
        {
            var jump = Vector2.Distance(palmTarget, smoothedPalm);
            if (jump <= MaxPalmJumpPerFrame)
            {
                return false;
            }

            return IsNearHorizontalEdge(palmTarget) || IsNearHorizontalEdge(smoothedPalm);
        }

        private static bool IsNearHorizontalEdge(Vector2 point)
        {
            return point.x <= EdgeInstabilityMargin || point.x >= 1f - EdgeInstabilityMargin;
        }

        private static Vector2 GetEdgeRotationPalm(Vector2 palmTarget)
        {
            var x = palmTarget.x < 0.5f ? EdgeRotationAnchorInset : 1f - EdgeRotationAnchorInset;
            return new Vector2(x, Mathf.Clamp01(palmTarget.y));
        }

        private bool TryContinueOpenHandRotationDuringDropout()
        {
            if (!calibrated || calibrationVisible || gestureWarmupTimer > 0f || openHandDropoutTimer <= 0f)
            {
                return false;
            }

            openHandDropoutTimer = Mathf.Max(0f, openHandDropoutTimer - Time.deltaTime);
            ApplyRotationFromPalm(lastOpenHandPalm);
            return true;
        }

        private void ApplyRotationFromPalm(Vector2 palmPosition)
        {
            var offset = palmPosition.x - calibratedCenterX;
            var magnitude = Mathf.Abs(offset);
            if (magnitude <= CenterDeadZone)
            {
                smoothedRotation = Mathf.Lerp(smoothedRotation, 0f, 1f - Mathf.Exp(-10f * Time.deltaTime));
                return;
            }

            var normalized = Mathf.Clamp01((magnitude - CenterDeadZone) / Mathf.Max(0.001f, 0.5f - CenterDeadZone));
            var speed = Smooth01(normalized) * MaxRotationDegreesPerFrame;
            var target = Mathf.Sign(offset) * speed;
            smoothedRotation = Mathf.Lerp(smoothedRotation, target, 1f - Mathf.Exp(-9f * Time.deltaTime));
            if (Mathf.Abs(smoothedRotation) > 0.02f)
            {
                RotationRequested?.Invoke(smoothedRotation);
            }
        }

        private void UpdateGestureHover()
        {
            if (currentState.IsIndexPoint && selectionCooldownTimer <= 0f)
            {
                gestureHoverActive = true;
                hoverGraceTimer = HoverGraceDuration;
                confirmFeedbackTimer = 0f;
                GestureHoverMoved?.Invoke(ToScreenPosition(smoothedGestureCursor));
                return;
            }

            if (currentState.IsThreeFingerPinch)
            {
                gestureHoverActive = true;
                hoverGraceTimer = Mathf.Max(hoverGraceTimer, PinchConfirmDuration);
                GestureHoverMoved?.Invoke(ToScreenPosition(smoothedGestureCursor));
                return;
            }

            DecayGestureHoverGrace();
            if (!currentState.IsThreeFingerPinch)
            {
                ClearGestureHoverIfExpired();
            }
        }

        private void UpdatePinchSelection()
        {
            if (selectionCooldownTimer > 0f)
            {
                return;
            }

            if (!currentState.IsThreeFingerPinch)
            {
                pinchTimer = Mathf.Max(0f, pinchTimer - Time.deltaTime * 2f);
                return;
            }

            if (!gestureHoverActive || hoverGraceTimer <= 0f)
            {
                pinchTimer = 0f;
                return;
            }

            pinchTimer += Time.deltaTime;
            if (pinchTimer < PinchConfirmDuration)
            {
                return;
            }

            pinchTimer = 0f;
            selectionCooldownTimer = SelectionCooldown;
            confirmFeedbackTimer = ConfirmFeedbackDuration;
            GestureConfirmRequested?.Invoke();
            ClearGestureHoverIfNeeded();
        }

        private void UpdateUiHoldConfirm()
        {
            if (currentState.IsOpenHand || currentState.IsIndexPoint || currentState.IsThreeFingerPinch || selectionCooldownTimer > 0f)
            {
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 1.5f);
                return;
            }

            holdTimer += Time.deltaTime;
            if (holdTimer < HoldConfirmDuration)
            {
                return;
            }

            holdTimer = 0f;
            selectionCooldownTimer = SelectionCooldown;
            DispatchHoldConfirm();
        }

        private bool DispatchHoldConfirm()
        {
            var screenPosition = new Vector2(
                Mathf.Lerp(0f, Screen.width, smoothedPalm.x),
                Mathf.Lerp(0f, Screen.height, smoothedPalm.y));
            var handlers = HoldConfirmRequested?.GetInvocationList();
            if (handlers == null)
            {
                return false;
            }

            var handled = false;
            for (var index = 0; index < handlers.Length; index++)
            {
                if (handlers[index] is Func<Vector2, bool> typedHandler)
                {
                    handled |= typedHandler(screenPosition);
                }
            }

            return handled;
        }

        private void ResetTimers()
        {
            calibrationTimer = 0f;
            holdTimer = 0f;
            pinchTimer = 0f;
            hoverGraceTimer = 0f;
            openHandDropoutTimer = 0f;
            confirmFeedbackTimer = 0f;
            selectionCooldownTimer = 0f;
            gestureHoverActive = false;
        }

        private static Vector2 MirrorForPlayer(Vector2 cameraPoint)
        {
            return new Vector2(1f - cameraPoint.x, 1f - cameraPoint.y);
        }

        private void ClearGestureHoverIfNeeded()
        {
            if (!gestureHoverActive)
            {
                return;
            }

            gestureHoverActive = false;
            pinchTimer = 0f;
            hoverGraceTimer = 0f;
            GestureHoverCleared?.Invoke();
        }

        private void DecayGestureHoverGrace()
        {
            if (!gestureHoverActive)
            {
                return;
            }

            hoverGraceTimer = Mathf.Max(0f, hoverGraceTimer - Time.deltaTime);
        }

        private void ClearGestureHoverIfExpired()
        {
            if (hoverGraceTimer > 0f)
            {
                return;
            }

            ClearGestureHoverIfNeeded();
        }

        private static Vector2 ToScreenPosition(Vector2 normalizedPosition)
        {
            return new Vector2(
                Mathf.Lerp(0f, Screen.width, normalizedPosition.x),
                Mathf.Lerp(0f, Screen.height, normalizedPosition.y));
        }

        private void OnGUI()
        {
            if (!gestureEnabled)
            {
                DrawStatusMessage();
                return;
            }

            DrawGestureFeedback();
            if (calibrationVisible)
            {
                DrawCalibrationOverlay();
            }

            if (showDebugOverlay && (Application.isEditor || Debug.isDebugBuild))
            {
                DrawDebugOverlay();
            }
        }

        private void DrawGestureFeedback()
        {
            EnsureFeedbackTextures();
            var useCursorFeedback = gestureHoverActive || confirmFeedbackTimer > 0f;
            var feedbackPosition = useCursorFeedback ? smoothedGestureCursor : smoothedPalm;
            var screenX = Mathf.Lerp(0f, Screen.width, feedbackPosition.x);
            var screenY = Mathf.Lerp(0f, Screen.height, feedbackPosition.y);
            var progress = calibrationVisible
                ? Mathf.Clamp01(calibrationTimer / CalibrationDuration)
                : Mathf.Max(
                    Mathf.Clamp01(pinchTimer / PinchConfirmDuration),
                    Mathf.Clamp01(confirmFeedbackTimer / ConfirmFeedbackDuration),
                    Mathf.Clamp01(holdTimer / HoldConfirmDuration));
            var size = Mathf.Lerp(58f, 78f, progress);
            GUI.color = new Color(1f, 1f, 1f, hasHand ? 0.48f : 0.12f);
            GUI.DrawTexture(new Rect(screenX - size * 0.5f, screenY - size * 0.5f, size, size), feedbackCircleTexture);
            if (progress > 0.01f)
            {
                GUI.color = new Color(0.92f, 0.74f, 0.38f, 0.76f);
                GUI.DrawTexture(new Rect(screenX - size * 0.56f, screenY - size * 0.56f, size * 1.12f, size * 1.12f), feedbackRingTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawCalibrationOverlay()
        {
            EnsureFeedbackTextures();
            var axisX = Screen.width * 0.5f;
            GUI.color = new Color(0.88f, 0.72f, 0.36f, 0.45f);
            GUI.DrawTexture(new Rect(axisX - 1.5f, 0f, 3f, Screen.height), Texture2D.whiteTexture);

            var targetY = Screen.height * 0.5f;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(new Rect(axisX - 66f, targetY - 66f, 132f, 132f), feedbackCircleTexture);
            GUI.color = new Color(0.92f, 0.74f, 0.38f, 0.8f * Mathf.Clamp01(calibrationTimer / CalibrationDuration));
            GUI.DrawTexture(new Rect(axisX - 76f, targetY - 76f, 152f, 152f), feedbackRingTexture);

            var rect = new Rect(Screen.width * 0.5f - 300f, 68f, 600f, 114f);
            GUI.color = new Color(1f, 1f, 1f, 0.94f);
            GUI.DrawTexture(rect, onboardingPanelTexture);
            GUI.color = new Color(0.9f, 0.88f, 0.8f, 1f);
            GUI.Label(new Rect(rect.x + 28f, rect.y + 18f, rect.width - 56f, 28f), "手势校准");
            GUI.color = new Color(0.78f, 0.8f, 0.78f, 1f);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 50f, rect.width - 56f, 52f),
                GetCalibrationHint());
            GUI.color = Color.white;
        }

        private string GetCalibrationHint()
        {
            if (!hasHand)
            {
                return "请把张开的手举到摄像头前。";
            }

            var inAxis = Mathf.Abs(smoothedPalm.x - 0.5f) <= CalibrationHorizontalTolerance &&
                smoothedPalm.y > 0.25f &&
                smoothedPalm.y < 0.78f;
            if (!currentState.IsOpenHand)
            {
                return $"已识别手部，但只判断到 {currentState.ExtendedFingerCount} 根手指张开。请把五指完全分开。";
            }

            return inAxis
                ? "很好，保持一秒完成校准。"
                : "五指已识别，把手心移到屏幕中间 y 轴目标圈内。";
        }

        private void DrawDebugOverlay()
        {
            var state = hasHand
                ? $"hand open:{currentState.IsOpenHand} index:{currentState.IsIndexPoint} pinch:{currentState.IsThreeFingerPinch} fingers:{currentState.ExtendedFingerCount} hover:{gestureHoverActive} grace:{hoverGraceTimer:0.00} x:{smoothedPalm.x:0.00} axis:{calibratedCenterX:0.00} pinchProgress:{pinchTimer / PinchConfirmDuration:0.00} rot:{smoothedRotation:0.00}"
                : "hand: none";
            GUI.color = new Color(0.86f, 0.86f, 0.82f, 0.9f);
            GUI.Label(new Rect(18f, Screen.height - 82f, 660f, 64f), state);
            GUI.color = Color.white;
        }

        private void DrawStatusMessage()
        {
            if (statusMessageTimer <= 0f || string.IsNullOrEmpty(statusMessage))
            {
                return;
            }

            EnsureFeedbackTextures();
            var rect = new Rect(Screen.width * 0.5f - 230f, 78f, 460f, 46f);
            GUI.color = new Color(0.02f, 0.024f, 0.032f, 0.9f);
            GUI.DrawTexture(rect, onboardingPanelTexture);
            GUI.color = new Color(0.86f, 0.82f, 0.72f, Mathf.Clamp01(statusMessageTimer / 0.6f));
            GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 24f), statusMessage);
            GUI.color = Color.white;
        }

        private void EnsureFeedbackTextures()
        {
            feedbackCircleTexture ??= CreateCircleTexture(96, 0.18f, 1f);
            feedbackRingTexture ??= CreateRingTexture(112, 0.72f, 0.88f);
            onboardingPanelTexture ??= CreateRoundedRectTexture(32, 32, 9, new Color(0.018f, 0.022f, 0.03f, 1f));
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static Texture2D CreateCircleTexture(int size, float innerGlow, float edge)
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
                    var alpha = Mathf.Clamp01((1f - distance) * innerGlow + Mathf.Clamp01(1f - Mathf.Abs(distance - edge) * 16f) * 0.82f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRingTexture(int size, float inner, float outer)
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
                    var alpha = distance >= inner && distance <= outer ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRoundedRectTexture(int width, int height, int radius, Color color)
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
            return texture;
        }
    }
}
