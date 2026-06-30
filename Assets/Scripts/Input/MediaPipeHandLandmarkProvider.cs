using System.Collections;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using UnityEngine;

namespace Tarot.Input
{
    public sealed class MediaPipeHandLandmarkProvider : MonoBehaviour, IHandLandmarkProvider
    {
        private const string ModelAssetName = "hand_landmarker.bytes";
        private const float MissingHandGraceSeconds = 0.18f;

        [SerializeField] private string preferredCameraName;
        [SerializeField] private int requestedWidth = 640;
        [SerializeField] private int requestedHeight = 480;
        [SerializeField] private int requestedFps = 30;
        [SerializeField] private int detectEveryFrames = 2;
        [SerializeField, Range(0.35f, 0.9f)] private float minHandDetectionConfidence = 0.62f;
        [SerializeField, Range(0.35f, 0.9f)] private float minHandPresenceConfidence = 0.62f;
        [SerializeField, Range(0.35f, 0.9f)] private float minTrackingConfidence = 0.58f;

        private readonly HandLandmark[] landmarks = new HandLandmark[21];
        private HandLandmarker handLandmarker;
        private WebCamTexture cameraTexture;
        private TextureFrame textureFrame;
        private Coroutine trackingRoutine;
        private HandLandmarkFrame latestFrame;
        private bool hasLatestFrame;
        private bool isTracking;
        private string statusMessage = "MediaPipe 手势识别未启动";
        private float missingHandTimer;
        private long lastTimestampMillis;

        public bool IsAvailable => WebCamTexture.devices.Length > 0;
        public string StatusMessage => statusMessage;

        private void OnDisable()
        {
            StopTracking();
        }

        public bool StartTracking()
        {
            if (isTracking)
            {
                return true;
            }

            if (!IsAvailable)
            {
                statusMessage = "没有检测到可用摄像头";
                return false;
            }

            isTracking = true;
            hasLatestFrame = false;
            missingHandTimer = 0f;
            statusMessage = "正在启动 MediaPipe 手势识别";
            trackingRoutine = StartCoroutine(TrackHands());
            return true;
        }

        public void StopTracking()
        {
            isTracking = false;
            hasLatestFrame = false;

            if (trackingRoutine != null)
            {
                StopCoroutine(trackingRoutine);
                trackingRoutine = null;
            }

            textureFrame?.Dispose();
            textureFrame = null;
            handLandmarker?.Close();
            handLandmarker = null;

            if (cameraTexture != null)
            {
                if (cameraTexture.isPlaying)
                {
                    cameraTexture.Stop();
                }

                Destroy(cameraTexture);
                cameraTexture = null;
            }

            statusMessage = "MediaPipe 手势识别已关闭";
        }

        public bool TryGetLatestFrame(out HandLandmarkFrame frame)
        {
            frame = latestFrame;
            return isTracking && hasLatestFrame && latestFrame.IsValid;
        }

        private IEnumerator TrackHands()
        {
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
                if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                {
                    statusMessage = "摄像头权限未开启";
                    StopTracking();
                    yield break;
                }
            }

            yield return PrepareModelAsset();
            if (!isTracking)
            {
                yield break;
            }

            var cameraName = SelectCameraName();
            cameraTexture = string.IsNullOrEmpty(cameraName)
                ? new WebCamTexture(requestedWidth, requestedHeight, requestedFps)
                : new WebCamTexture(cameraName, requestedWidth, requestedHeight, requestedFps);
            cameraTexture.Play();
            statusMessage = "等待摄像头画面";

            var startupDeadline = Time.realtimeSinceStartup + 5f;
            while (isTracking && Time.realtimeSinceStartup < startupDeadline)
            {
                if (cameraTexture.width > 32 && cameraTexture.height > 32)
                {
                    break;
                }

                yield return null;
            }

            if (!isTracking)
            {
                yield break;
            }

            if (cameraTexture.width <= 32 || cameraTexture.height <= 32)
            {
                statusMessage = "摄像头画面启动失败";
                StopTracking();
                yield break;
            }

            textureFrame = new TextureFrame(cameraTexture.width, cameraTexture.height, TextureFormat.RGBA32);
            handLandmarker = CreateHandLandmarker();
            var result = HandLandmarkerResult.Alloc(1);
            var processingOptions = new ImageProcessingOptions();
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var frameIndex = 0;
            statusMessage = "把张开的手放到屏幕中间校准";

            while (isTracking)
            {
                yield return waitForEndOfFrame;

                frameIndex++;
                if (detectEveryFrames > 1 && frameIndex % detectEveryFrames != 0)
                {
                    continue;
                }

                textureFrame.ReadTextureOnCPU(cameraTexture);
                using var image = textureFrame.BuildCPUImage();
                var timestampMillis = GetMonotonicTimestampMillis();
                var found = handLandmarker.TryDetectForVideo(image, timestampMillis, processingOptions, ref result);
                textureFrame.Release();

                if (found && TryConvertResult(result, timestampMillis))
                {
                    missingHandTimer = 0f;
                    statusMessage = "已识别手部";
                    continue;
                }

                missingHandTimer += Time.unscaledDeltaTime;
                if (missingHandTimer >= MissingHandGraceSeconds)
                {
                    hasLatestFrame = false;
                    statusMessage = "未识别到手部";
                }
            }
        }

        private IEnumerator PrepareModelAsset()
        {
#if UNITY_EDITOR
            IResourceManager resourceManager = new LocalResourceManager("TarotMediaPipe");
#else
            IResourceManager resourceManager = new StreamingAssetsResourceManager("MediaPipe");
#endif
            statusMessage = "正在准备 MediaPipe 手部模型";
            yield return resourceManager.PrepareAssetAsync(ModelAssetName, ModelAssetName, false);
        }

        private HandLandmarker CreateHandLandmarker()
        {
            var baseOptions = new BaseOptions(
                BaseOptions.Delegate.CPU,
                modelAssetPath: ModelAssetName);
            var options = new HandLandmarkerOptions(
                baseOptions,
                RunningMode.VIDEO,
                numHands: 1,
                minHandDetectionConfidence: minHandDetectionConfidence,
                minHandPresenceConfidence: minHandPresenceConfidence,
                minTrackingConfidence: minTrackingConfidence);
            return HandLandmarker.CreateFromOptions(options);
        }

        private bool TryConvertResult(HandLandmarkerResult result, long timestampMillis)
        {
            if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            {
                return false;
            }

            var normalized = result.handLandmarks[0].landmarks;
            if (normalized == null || normalized.Count < landmarks.Length)
            {
                return false;
            }

            var confidence = GetHandConfidence(result);
            for (var index = 0; index < landmarks.Length; index++)
            {
                var point = normalized[index];
                landmarks[index] = new HandLandmark(
                    new Vector3(
                        Mathf.Clamp01(point.x),
                        Mathf.Clamp01(point.y),
                        point.z),
                    point.visibility ?? point.presence ?? confidence);
            }

            latestFrame = new HandLandmarkFrame(
                (HandLandmark[])landmarks.Clone(),
                confidence,
                timestampMillis / 1000.0);
            hasLatestFrame = latestFrame.IsValid;
            return hasLatestFrame;
        }

        private static float GetHandConfidence(HandLandmarkerResult result)
        {
            if (result.handedness == null ||
                result.handedness.Count == 0 ||
                result.handedness[0].categories == null ||
                result.handedness[0].categories.Count == 0)
            {
                return 0.75f;
            }

            return Mathf.Clamp01(result.handedness[0].categories[0].score);
        }

        private string SelectCameraName()
        {
            var devices = WebCamTexture.devices;
            if (!string.IsNullOrWhiteSpace(preferredCameraName))
            {
                for (var index = 0; index < devices.Length; index++)
                {
                    if (devices[index].name.Contains(preferredCameraName))
                    {
                        return devices[index].name;
                    }
                }
            }

            return devices.Length > 0 ? devices[0].name : string.Empty;
        }

        private long GetMonotonicTimestampMillis()
        {
            var timestamp = (long)(Time.realtimeSinceStartup * 1000L);
            if (timestamp <= lastTimestampMillis)
            {
                timestamp = lastTimestampMillis + 1;
            }

            lastTimestampMillis = timestamp;
            return timestamp;
        }
    }
}
