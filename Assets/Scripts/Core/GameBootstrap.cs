using UnityEngine;
using Tarot.Appearance;
using Tarot.UI;

namespace Tarot.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string firstSceneName = "Boot";

        private void Awake()
        {
            Application.targetFrameRate = 60;
            EnsureCoreObjects();
            Debug.Log($"Tarot bootstrap initialized. First scene: {firstSceneName}");
        }

        private void EnsureCoreObjects()
        {
            var backgroundRoot = new GameObject("Background Manager");
            var backgroundManager = backgroundRoot.AddComponent<BackgroundManager>();

            var starfieldObject = new GameObject("Default Starfield Background");
            starfieldObject.transform.SetParent(backgroundRoot.transform, false);
            var starfield = starfieldObject.AddComponent<DefaultStarfieldBackground>();
            backgroundManager.SetActiveBackground(starfield);

            var menuObject = new GameObject("Main Menu");
            menuObject.AddComponent<MainMenuController>();
        }
    }
}
