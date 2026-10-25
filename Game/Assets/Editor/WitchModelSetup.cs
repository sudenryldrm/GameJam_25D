using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WitchModelSetup
{
    private const string ScenePath = "Assets/Scene2_WitchChase.unity";
    private const string PrefabPath = "Assets/Models/QuaterniusWitch/QuaterniusWitchVisual.prefab";

    public static void InstallBatch()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Install();
    }

    [MenuItem("Tools/Witch/Install Flying Witch in Scene 2")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open Scene2_WitchChase first.");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new InvalidOperationException("Missing Quaternius witch prefab.");
        WitchBossAI boss = UnityEngine.Object.FindFirstObjectByType<WitchBossAI>();
        if (boss == null) throw new InvalidOperationException("WitchBossAI not found.");
        Transform anchor = boss.transform.Find("Witch_ArtAnchor");
        if (anchor == null)
        {
            var go = new GameObject("Witch_ArtAnchor");
            Undo.RegisterCreatedObjectUndo(go, "Witch art anchor");
            anchor = go.transform;
            Undo.SetTransformParent(anchor, boss.transform, "Parent witch art");
            anchor.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        Transform previous = anchor.Find("Witch_ImportedVisual");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(visual, "Install colored witch");
        Vector3 position = visual.transform.localPosition;
        Quaternion rotation = visual.transform.localRotation;
        Vector3 scale = visual.transform.localScale;
        Undo.SetTransformParent(visual.transform, anchor, "Parent colored witch");
        visual.name = "Witch_ImportedVisual";
        visual.transform.SetLocalPositionAndRotation(position, rotation);
        visual.transform.localScale = scale;
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.avatar == null || !animator.avatar.isValid ||
            visual.GetComponent<WitchFlightVisual>() == null ||
            visual.GetComponentsInChildren<Renderer>(true).Length == 0)
        {
            Undo.DestroyObjectImmediate(visual);
            throw new InvalidOperationException("Colored witch prefab is missing its rig, renderer or animation player.");
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
        MeshRenderer capsule = boss.GetComponent<MeshRenderer>();
        if (capsule != null)
        {
            Undo.RecordObject(capsule, "Hide witch capsule");
            capsule.enabled = false;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Colored witch installed in Scene 2; AI and apple mechanics remain on WitchBoss.");
    }
}
