using UnityEngine;

namespace Tarot.Input
{
    public readonly struct HandGestureState
    {
        public HandGestureState(
            bool hasHand,
            bool isOpenHand,
            bool isIndexPoint,
            bool isThreeFingerPinch,
            bool isFist,
            Vector2 palmCenter,
            Vector2 pointerCenter,
            Vector2 pinchCenter,
            float handArea,
            float depth,
            int extendedFingerCount,
            float confidence)
        {
            HasHand = hasHand;
            IsOpenHand = isOpenHand;
            IsIndexPoint = isIndexPoint;
            IsThreeFingerPinch = isThreeFingerPinch;
            IsFist = isFist;
            PalmCenter = palmCenter;
            PointerCenter = pointerCenter;
            PinchCenter = pinchCenter;
            HandArea = handArea;
            Depth = depth;
            ExtendedFingerCount = extendedFingerCount;
            Confidence = confidence;
        }

        public bool HasHand { get; }
        public bool IsOpenHand { get; }
        public bool IsIndexPoint { get; }
        public bool IsThreeFingerPinch { get; }
        public bool IsFist { get; }
        public Vector2 PalmCenter { get; }
        public Vector2 PointerCenter { get; }
        public Vector2 PinchCenter { get; }
        public float HandArea { get; }
        public float Depth { get; }
        public int ExtendedFingerCount { get; }
        public float Confidence { get; }
    }

    public sealed class HandGestureRecognizer
    {
        public HandGestureState Analyze(HandLandmarkFrame frame)
        {
            if (!frame.IsValid)
            {
                return default;
            }

            var wrist = Get(frame, HandLandmarkId.Wrist);
            var indexMcp = Get(frame, HandLandmarkId.IndexMcp);
            var middleMcp = Get(frame, HandLandmarkId.MiddleMcp);
            var ringMcp = Get(frame, HandLandmarkId.RingMcp);
            var pinkyMcp = Get(frame, HandLandmarkId.PinkyMcp);
            var palm = (wrist + indexMcp + middleMcp + ringMcp + pinkyMcp) / 5f;
            var palm2 = new Vector2(palm.x, palm.y);

            var handScale = Mathf.Max(0.001f, Vector2.Distance(To2(wrist), To2(middleMcp)));
            var thumbExtended = IsThumbExtended(frame, palm, handScale);
            var indexExtended = IsFingerExtended(frame, HandLandmarkId.IndexMcp, HandLandmarkId.IndexPip, HandLandmarkId.IndexDip, HandLandmarkId.IndexTip, palm, handScale);
            var middleExtended = IsFingerExtended(frame, HandLandmarkId.MiddleMcp, HandLandmarkId.MiddlePip, HandLandmarkId.MiddleDip, HandLandmarkId.MiddleTip, palm, handScale);
            var ringExtended = IsFingerExtended(frame, HandLandmarkId.RingMcp, HandLandmarkId.RingPip, HandLandmarkId.RingDip, HandLandmarkId.RingTip, palm, handScale);
            var pinkyExtended = IsFingerExtended(frame, HandLandmarkId.PinkyMcp, HandLandmarkId.PinkyPip, HandLandmarkId.PinkyDip, HandLandmarkId.PinkyTip, palm, handScale);
            var extended = 0;
            extended += thumbExtended ? 1 : 0;
            extended += indexExtended ? 1 : 0;
            extended += middleExtended ? 1 : 0;
            extended += ringExtended ? 1 : 0;
            extended += pinkyExtended ? 1 : 0;

            var fistTightness = AverageTipDistance(frame, palm) / handScale;
            var thumbTip = To2(Get(frame, HandLandmarkId.ThumbTip));
            var indexTip = To2(Get(frame, HandLandmarkId.IndexTip));
            var middleTip = To2(Get(frame, HandLandmarkId.MiddleTip));
            var ringTip = To2(Get(frame, HandLandmarkId.RingTip));
            var pinkyTip = To2(Get(frame, HandLandmarkId.PinkyTip));
            var indexTipPalm = Vector2.Distance(indexTip, palm2);
            var middleTipPalm = Vector2.Distance(middleTip, palm2);
            var ringTipPalm = Vector2.Distance(ringTip, palm2);
            var pinkyTipPalm = Vector2.Distance(pinkyTip, palm2);
            var middleFoldedForPoint = !middleExtended || middleTipPalm < indexTipPalm * 0.86f;
            var ringFoldedForPoint = !ringExtended || ringTipPalm < indexTipPalm * 0.9f;
            var pinkyFoldedForPoint = !pinkyExtended || pinkyTipPalm < indexTipPalm * 0.9f;
            var thumbIndexDistance = Vector2.Distance(thumbTip, indexTip);
            var thumbMiddleDistance = Vector2.Distance(thumbTip, middleTip);
            var indexMiddleDistance = Vector2.Distance(indexTip, middleTip);
            var thumbTouchesIndex = thumbIndexDistance <= handScale * 0.48f;
            var thumbTouchesMiddle = thumbMiddleDistance <= handScale * 0.56f;
            var indexMiddleCompact = indexMiddleDistance <= handScale * 0.52f;
            var isThreeFingerPinch =
                thumbTouchesIndex &&
                thumbTouchesMiddle &&
                (indexMiddleCompact || extended <= 3 || (!indexExtended && !middleExtended));
            var isIndexPoint =
                !isThreeFingerPinch &&
                indexExtended &&
                middleFoldedForPoint &&
                ringFoldedForPoint &&
                pinkyFoldedForPoint &&
                extended <= 3;
            var isOpenHand = extended >= 4 && !isIndexPoint && !isThreeFingerPinch;
            var isFist = !isOpenHand && !isIndexPoint && !isThreeFingerPinch && extended <= 3 && fistTightness < 2.34f;
            var pointerCenter = indexTip;
            var pinchCenter = (thumbTip + indexTip + middleTip) / 3f;
            var area = CalculateArea(frame);
            var depth = CalculateDepth(frame);
            return new HandGestureState(true, isOpenHand, isIndexPoint, isThreeFingerPinch, isFist, palm2, pointerCenter, pinchCenter, area, depth, extended, frame.Confidence);
        }

        private static Vector3 Get(HandLandmarkFrame frame, HandLandmarkId id)
        {
            return frame.TryGet(id, out var landmark) ? landmark.NormalizedPosition : Vector3.zero;
        }

        private static bool IsFingerExtended(
            HandLandmarkFrame frame,
            HandLandmarkId mcpId,
            HandLandmarkId pipId,
            HandLandmarkId dipId,
            HandLandmarkId tipId,
            Vector3 palm,
            float handScale)
        {
            var mcp = Get(frame, mcpId);
            var pip = Get(frame, pipId);
            var dip = Get(frame, dipId);
            var tip = Get(frame, tipId);
            var palm2 = To2(palm);
            var tipPalm = Vector2.Distance(To2(tip), palm2);
            var dipPalm = Vector2.Distance(To2(dip), palm2);
            var pipPalm = Vector2.Distance(To2(pip), palm2);
            var tipMcp = Vector2.Distance(To2(tip), To2(mcp));
            return
                tipPalm > dipPalm * 1.04f &&
                tipPalm > pipPalm * 1.08f &&
                tipMcp > handScale * 0.62f;
        }

        private static bool IsThumbExtended(HandLandmarkFrame frame, Vector3 palm, float handScale)
        {
            var tip = Get(frame, HandLandmarkId.ThumbTip);
            var ip = Get(frame, HandLandmarkId.ThumbIp);
            var mcp = Get(frame, HandLandmarkId.ThumbMcp);
            var palm2 = To2(palm);
            var tipPalm = Vector2.Distance(To2(tip), palm2);
            var ipPalm = Vector2.Distance(To2(ip), palm2);
            var tipMcp = Vector2.Distance(To2(tip), To2(mcp));
            return tipPalm > ipPalm * 1.03f && tipMcp > handScale * 0.44f;
        }

        private static float AverageTipDistance(HandLandmarkFrame frame, Vector3 palm)
        {
            var palm2 = To2(palm);
            var sum =
                Vector2.Distance(To2(Get(frame, HandLandmarkId.ThumbTip)), palm2) +
                Vector2.Distance(To2(Get(frame, HandLandmarkId.IndexTip)), palm2) +
                Vector2.Distance(To2(Get(frame, HandLandmarkId.MiddleTip)), palm2) +
                Vector2.Distance(To2(Get(frame, HandLandmarkId.RingTip)), palm2) +
                Vector2.Distance(To2(Get(frame, HandLandmarkId.PinkyTip)), palm2);
            return sum / 5f;
        }

        private static Vector2 To2(Vector3 value)
        {
            return new Vector2(value.x, value.y);
        }

        private static float CalculateArea(HandLandmarkFrame frame)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var index = 0; index < frame.Landmarks.Length; index++)
            {
                var point = frame.Landmarks[index].NormalizedPosition;
                min = Vector2.Min(min, new Vector2(point.x, point.y));
                max = Vector2.Max(max, new Vector2(point.x, point.y));
            }

            var size = max - min;
            return Mathf.Max(0f, size.x * size.y);
        }

        private static float CalculateDepth(HandLandmarkFrame frame)
        {
            var depth = 0f;
            for (var index = 0; index < frame.Landmarks.Length; index++)
            {
                depth += frame.Landmarks[index].NormalizedPosition.z;
            }

            return depth / frame.Landmarks.Length;
        }
    }
}
