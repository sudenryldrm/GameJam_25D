using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class OpeningPreviewBaker
{
    private const string Folder = "Assets/Resources/MapPreviews";
    [InitializeOnLoadMethod]
    private static void Queue()
    {
        EditorApplication.delayCall += AutoBake;
        EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += AutoBake; };
    }
    private static void AutoBake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (File.Exists(Folder + "/Forest.prefab") && File.Exists(Folder + "/WitchForest.prefab") && File.Exists(Folder + "/Tower.prefab")) return;
        Bake();
    }

    [MenuItem("Tools/Story/Bake Opening Map Previews")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "MapPreviews");
        Scene original = SceneManager.GetActiveScene();
        string[] paths = { "Assets/SampleScene.unity", "Assets/Scene2_WitchChase.unity", "Assets/Scene3_TowerEscape.unity" };
        string[] names = { "Forest", "WitchForest", "Tower" };
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                Scene scene = SceneManager.GetSceneByPath(paths[i]);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
                var preview = new GameObject(names[i] + " Scenery Preview");
                SceneManager.MoveGameObjectToScene(preview, scene);
                var meshes = new Dictionary<Mesh, Mesh>();
                var materials = new Dictionary<Material, Material>();
                int count = 0;
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        if (root == preview) continue;
                        foreach (ForestLevelArt layout in root.GetComponentsInChildren<ForestLevelArt>())
                            layout.SendMessage("ApplyLayout", SendMessageOptions.DontRequireReceiver);
                        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(false))
                        {
                            if (!renderer.enabled || renderer.forceRenderingOff || !renderer.TryGetComponent(out MeshFilter filter) || !filter.sharedMesh) continue;
                            if (renderer.GetComponentInParent<PlayerController25D>() || renderer.GetComponentInParent<DwarfPursuer>() ||
                                renderer.GetComponentInParent<WitchBossAI>() || renderer.GetComponentInParent<PrinceChaserAI>()) continue;
                            var piece = new GameObject(renderer.name);
                            piece.transform.SetParent(preview.transform, false);
                            piece.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                            piece.transform.localScale = renderer.transform.lossyScale;
                            Mesh mesh = filter.sharedMesh;
                            if (!AssetDatabase.Contains(mesh))
                            {
                                if (!meshes.TryGetValue(mesh, out Mesh saved))
                                {
                                    saved = UnityEngine.Object.Instantiate(mesh); saved.hideFlags = HideFlags.None;
                                    string path = Folder + "/" + names[i] + "_Mesh_" + meshes.Count + ".asset";
                                    Mesh previous = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                                    if (previous) { EditorUtility.CopySerialized(saved, previous); UnityEngine.Object.DestroyImmediate(saved); saved = previous; }
                                    else AssetDatabase.CreateAsset(saved, path);
                                    meshes.Add(mesh, saved);
                                }
                                mesh = saved;
                            }
                            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                            Material[] slots = renderer.sharedMaterials;
                            for (int slot = 0; slot < slots.Length; slot++)
                            {
                                Material material = slots[slot];
                                if (!material || AssetDatabase.Contains(material)) continue;
                                if (!materials.TryGetValue(material, out Material saved))
                                {
                                    saved = UnityEngine.Object.Instantiate(material); saved.hideFlags = HideFlags.None;
                                    if (material.name.StartsWith("Mountain Haze")) saved.SetColor("_BaseColor", new Color(.23f, .3f, .39f));
                                    string path = Folder + "/" + names[i] + "_Material_" + materials.Count + ".mat";
                                    Material previous = AssetDatabase.LoadAssetAtPath<Material>(path);
                                    if (previous) { EditorUtility.CopySerialized(saved, previous); UnityEngine.Object.DestroyImmediate(saved); saved = previous; }
                                    else AssetDatabase.CreateAsset(saved, path);
                                    materials.Add(material, saved);
                                }
                                slots[slot] = saved;
                            }
                            var copy = piece.AddComponent<MeshRenderer>(); copy.sharedMaterials = slots;
                            copy.shadowCastingMode = ShadowCastingMode.Off; copy.receiveShadows = false;
                            count++;
                        }
                    }
                    var info = preview.AddComponent<MapPreviewInfo>();
                    info.sunlight = i == 0 ? new Color(1f, 211f / 255f, 144f / 255f) : new Color(.7f,.8f,1);
                    info.intensity = i == 0 ? 1.5625f : 1.15f;
                    info.sky = i == 0 ? new Color(.43f,.47f,.43f) : new Color(.3f,.36f,.49f);
                    info.ambient = i == 0 ? new Color(.32f,.34f,.27f) : new Color(.24f,.28f,.34f);
                    info.mist = i == 0 ? new Color(.49f,.53f,.4f) : new Color(.2f,.25f,.35f);
                    if (count < 10) throw new Exception(names[i] + ": scenery preview is incomplete");
                    PrefabUtility.SaveAsPrefabAsset(preview, Folder + "/" + names[i] + ".prefab");
                    Debug.Log("OPENING PREVIEW: " + names[i] + " baked with " + count + " scenery pieces and no gameplay scripts.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(preview);
                    if (opened) EditorSceneManager.CloseScene(scene, true);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("OPENING PREVIEWS READY: three maps, 15-second tour, no preview level gameplay.");
        }
        finally { if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
    }
}
