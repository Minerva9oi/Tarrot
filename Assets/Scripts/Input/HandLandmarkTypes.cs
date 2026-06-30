using UnityEngine;

namespace Tarot.Input
{
    public enum HandLandmarkId
    {
        Wrist = 0,
        ThumbCmc = 1,
        ThumbMcp = 2,
        ThumbIp = 3,
        ThumbTip = 4,
        IndexMcp = 5,
        IndexPip = 6,
        IndexDip = 7,
        IndexTip = 8,
        MiddleMcp = 9,
        MiddlePip = 10,
        MiddleDip = 11,
        MiddleTip = 12,
        RingMcp = 13,
        RingPip = 14,
        RingDip = 15,
        RingTip = 16,
        PinkyMcp = 17,
        PinkyPip = 18,
        PinkyDip = 19,
        PinkyTip = 20
    }

    public readonly struct HandLandmark
    {
        public HandLandmark(Vector3 normalizedPosition, float visibility = 1f)
        {
            NormalizedPosition = normalizedPosition;
            Visibility = visibility;
        }

        public Vector3 NormalizedPosition { get; }
        public float Visibility { get; }
    }

    public readonly struct HandLandmarkFrame
    {
        public HandLandmarkFrame(HandLandmark[] landmarks, float confidence, double timestampSeconds)
        {
            Landmarks = landmarks;
            Confidence = confidence;
            TimestampSeconds = timestampSeconds;
        }

        public HandLandmark[] Landmarks { get; }
        public float Confidence { get; }
        public double TimestampSeconds { get; }

        public bool IsValid => Landmarks != null && Landmarks.Length >= 21 && Confidence >= 0.5f;

        public bool TryGet(HandLandmarkId id, out HandLandmark landmark)
        {
            var index = (int)id;
            if (Landmarks != null && index >= 0 && index < Landmarks.Length)
            {
                landmark = Landmarks[index];
                return true;
            }

            landmark = default;
            return false;
        }
    }

    public interface IHandLandmarkProvider
    {
        bool IsAvailable { get; }
        string StatusMessage { get; }
        bool StartTracking();
        void StopTracking();
        bool TryGetLatestFrame(out HandLandmarkFrame frame);
    }
}
