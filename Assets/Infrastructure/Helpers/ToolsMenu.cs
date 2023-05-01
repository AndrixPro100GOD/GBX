using UnityEditor;
using UnityEngine;

namespace Infrastructure.Helpers
{
#if UNITY_EDITOR
    
    public class ToolsMenu : MonoBehaviour
    {
        private const string MenuScenePath = "Assets/_ProjectSurvival/Scenes/MenuScene.unity";
        private const string HordeScenePath = "Assets/_ProjectSurvival/Scenes/HordeScene.unity";

        [MenuItem("GBX/Drop State")]
        static void Init()
        {
            GameProgressHandler.DeleteFile();
        }

        /*[MenuItem("GBX/Scenes/Menu")]
        static void LoadMenu()
        {
            TryToOpenScene(MenuScenePath);
        }

        [MenuItem("GBX/Scenes/Horde")]
        static void LoadHorde()
        {
            TryToOpenScene(HordeScenePath);
        }*/


        /*private static void TryToOpenScene(string scenePath)
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        }*/
    }
#endif
}