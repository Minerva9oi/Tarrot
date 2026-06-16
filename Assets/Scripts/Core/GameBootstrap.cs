using UnityEngine;

namespace Tarot.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string firstSceneName = "Boot";

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Debug.Log($"Tarot bootstrap initialized. First scene: {firstSceneName}");
        }
    }
}

