using UnityEngine;

namespace Tarot.Appearance
{
    public sealed class BackgroundManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour activeBackground;

        public IReadingEffectBackground ActiveReadingBackground => activeBackground as IReadingEffectBackground;

        public void SetActiveBackground(MonoBehaviour background)
        {
            activeBackground = background;
        }

        public void SetIdle()
        {
            ActiveReadingBackground?.SetIdle();
        }

        public void Awaken()
        {
            ActiveReadingBackground?.Awaken();
        }

        public void GatherTo(Vector3 worldPosition)
        {
            ActiveReadingBackground?.GatherTo(worldPosition);
        }

        public void Restore()
        {
            ActiveReadingBackground?.Restore();
        }

        public void RotateStarfield(float degrees)
        {
            if (activeBackground is DefaultStarfieldBackground starfield)
            {
                starfield.RotateByDeckDegrees(degrees);
            }
        }

        public void TriggerRotationMeteorTrail(float degrees)
        {
            if (activeBackground is DefaultStarfieldBackground starfield)
            {
                starfield.TriggerRotationMeteorTrail(degrees);
            }
        }
    }
}
