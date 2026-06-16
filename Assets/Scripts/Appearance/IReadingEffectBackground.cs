using UnityEngine;

namespace Tarot.Appearance
{
    public interface IReadingEffectBackground
    {
        ReadingBackgroundState State { get; }
        void SetIdle();
        void Awaken();
        void GatherTo(Vector3 worldPosition);
        void Restore();
    }
}
