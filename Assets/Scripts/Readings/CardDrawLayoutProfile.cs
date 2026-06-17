using System;
using UnityEngine;

namespace Tarot.Readings
{
    public interface ICardDrawLayoutProvider
    {
        CardDrawLayoutProfile DrawLayout { get; }
    }

    [Serializable]
    public sealed class CardDrawLayoutProfile
    {
        [SerializeField] private float ringCardScale = 1.55f;
        [SerializeField] private float ringRadiusHalfHeightMultiplier = 6.2f;
        [SerializeField] private float ringCenterYOffsetHalfHeightMultiplier = -5.64f;
        [SerializeField] private float visibleArcDegrees = 35f;
        [SerializeField] private float selectedCardScale = 1.55f;
        [SerializeField] private Vector2 selectedCardViewportPosition = new(0.5f, 0.4f);
        [SerializeField] private float edgeCropAllowance = 0.8f;

        public float RingCardScale => ringCardScale;
        public float RingRadiusHalfHeightMultiplier => ringRadiusHalfHeightMultiplier;
        public float RingCenterYOffsetHalfHeightMultiplier => ringCenterYOffsetHalfHeightMultiplier;
        public float VisibleArcDegrees => visibleArcDegrees;
        public float SelectedCardScale => selectedCardScale;
        public Vector2 SelectedCardViewportPosition => selectedCardViewportPosition;
        public float EdgeCropAllowance => edgeCropAllowance;

        public CardDrawLayoutProfile()
        {
        }

        public CardDrawLayoutProfile(
            float ringCardScale,
            float ringRadiusHalfHeightMultiplier,
            float ringCenterYOffsetHalfHeightMultiplier,
            float visibleArcDegrees,
            float selectedCardScale,
            Vector2 selectedCardViewportPosition,
            float edgeCropAllowance = 0.55f)
        {
            this.ringCardScale = Mathf.Max(0.01f, ringCardScale);
            this.ringRadiusHalfHeightMultiplier = Mathf.Max(0.01f, ringRadiusHalfHeightMultiplier);
            this.ringCenterYOffsetHalfHeightMultiplier = ringCenterYOffsetHalfHeightMultiplier;
            this.visibleArcDegrees = Mathf.Clamp(visibleArcDegrees, 1f, 180f);
            this.selectedCardScale = Mathf.Max(0.01f, selectedCardScale);
            this.selectedCardViewportPosition = selectedCardViewportPosition;
            this.edgeCropAllowance = Mathf.Max(0f, edgeCropAllowance);
        }

        public static CardDrawLayoutProfile CreateDefault()
        {
            return new CardDrawLayoutProfile();
        }
    }
}
