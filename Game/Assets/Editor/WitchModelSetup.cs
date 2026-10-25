using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WitchModelSetup
{
    [MenuItem("Tools/Witch/Install Flying Witch in Scene 2")]
    public static void Install()
    {
        const string modelPath = "Assets/Models/Witch/Sketchfab.fbx";
        const string flightPath = "Assets/Models/Witch/WitchFlying.fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var flightModel = AssetDatabase.LoadAssetAtPath<GameObject>(flightPath);
        if (model == null || flightModel == null)
        {
            Debug.LogError("Copy Sketchfab.fbx and WitchFlying.fbx into Assets/Models/Witch/ first.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.name.Contains("Scene2_WitchChase"))
        {
            Debug.LogError("Open Scene2_WitchChase before running the witch installer.");
            return;
        }
        var boss = Object.FindFirstObjectByType<WitchBossAI>();
        if (boss == null) { Debug.LogError("WitchBossAI not found."); return; }
        Transform anchor = boss.transform.Find("Witch_ArtAnchor");
        if (anchor == null)
        {
            var anchorObject = new GameObject("Witch_ArtAnchor");
            anchor = anchorObject.transform;
            Undo.SetTransformParent(anchor, boss.transform, "Witch anchor");
            anchor.localPosition = Vector3.zero;
        }
        Transform previous = anchor.Find("Witch_ImportedVisual");
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        Undo.RegisterCreatedObjectUndo(visual, "Install witch model");
        visual.name = "Witch_ImportedVisual";
        Undo.SetTransformParent(visual.transform, anchor, "Parent witch model");
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        AnimationClip flight = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(flightPath))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) { flight = clip; break; }
        if (flight != null)
        {
            var targetAnimator = visual.GetComponentInChildren<Animator>(true);
            if (targetAnimator != null)
            {
                var flightPlayer = Undo.AddComponent<WitchFlightVisual>(targetAnimator.gameObject);
                flightPlayer.SetClip(flight);
                EditorUtility.SetDirty(flightPlayer);
            }
            else Debug.LogWarning("Imported model has no Animator; animation needs rig setup.");
        }
        else Debug.LogWarning("No animation clip found in WitchFlying.fbx.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Witch visual installed and Scene 2 saved. Check materials, orientation and animation in Play Mode.");
    }
}
