using System.IO;
using Tarot.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tarot.EditorTools
{
    public static class TarotProjectBootstrapper
    {
        public static void CreateBootScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            DefaultCardDeckArtBuilder.CreateOrUpdate();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Boot";

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.006f, 0.01f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.tag = "MainCamera";

            var bootstrapObject = new GameObject("Game Bootstrap");
            bootstrapObject.AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boot.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
