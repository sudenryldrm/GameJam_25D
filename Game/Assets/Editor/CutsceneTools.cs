using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public static class CutsceneTools
{
    [MenuItem("GameJam/Cutscenes/Open Current Scene Timeline")]
    private static void OpenTimeline()
    {
        foreach (PlayableDirector director in Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None))
        {
            if (director.gameObject.scene != SceneManager.GetActiveScene() || director.playableAsset == null) continue;
            Selection.activeGameObject = director.gameObject;
            EditorGUIUtility.PingObject(director.playableAsset);
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            return;
        }
        Debug.LogWarning("Open SampleScene, Scene2_WitchChase or Scene3_TowerEscape first.");
    }
}
