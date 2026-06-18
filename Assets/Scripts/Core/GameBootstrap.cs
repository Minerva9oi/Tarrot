using UnityEngine;
using Tarot.Appearance;
using Tarot.DailyReading;
using Tarot.SpreadReading;
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
            var mainMenu = menuObject.AddComponent<MainMenuController>();
            mainMenu.DailyReadingRequested += () =>
            {
                menuObject.SetActive(false);
                var dailyObject = new GameObject("Daily Reading");
                var dailyReading = dailyObject.AddComponent<DailyReadingController>();
                dailyReading.SetBackgroundManager(backgroundManager);
                dailyReading.BackRequested += () =>
                {
                    Destroy(dailyObject);
                    menuObject.SetActive(true);
                    backgroundManager.SetIdle();
                };
            };
            mainMenu.ThreeCardReadingRequested += () =>
            {
                menuObject.SetActive(false);
                var threeCardObject = new GameObject("Three Card Reading");
                var threeCardReading = threeCardObject.AddComponent<ThreeCardReadingController>();
                threeCardReading.SetBackgroundManager(backgroundManager);
                threeCardReading.BackRequested += () =>
                {
                    Destroy(threeCardObject);
                    menuObject.SetActive(true);
                    backgroundManager.SetIdle();
                };
            };
        }
    }
}
