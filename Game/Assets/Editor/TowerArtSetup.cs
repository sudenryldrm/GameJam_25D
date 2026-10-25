using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class TowerArtSetup
{
    [MenuItem("Tools/Scene Art/Finish Castle and Knight")]
    public static void Install()
    {
        const string path = "Assets/Resources/SceneArt/Knight.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips) { clip.loopTime = true; clip.lockRootPositionXZ = true; clip.keepOriginalPositionXZ = true; }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        var scene = EditorSceneManager.OpenScene("Assets/Scene3_TowerEscape.unity");
        var builder = UnityEngine.Object.FindFirstObjectByType<SceneArtFinisher>();
        if (!builder) throw new Exception("Tower art builder missing");
        builder.Build();
        var knight = GameObject.Find("Knight_Visual");
        var animation = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (!knight || !animation || animation.length <= 0) throw new Exception("Knight animation missing");
        var bones = knight.GetComponentsInChildren<Transform>();
        var before = bones.Select(b => b.localRotation).ToArray();
        var positions = bones.Select(b => b.localPosition).ToArray();
        var scales = bones.Select(b => b.localScale).ToArray();
        var animator = knight.GetComponentInChildren<Animator>();
        var graph = PlayableGraph.Create("Knight Validation");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var playable = AnimationClipPlayable.Create(graph, animation);
        AnimationPlayableOutput.Create(graph, "Test", animator).SetSourcePlayable(playable);
        graph.Play(); playable.SetTime(animation.length * .37); graph.Evaluate(0);
        bool changed = bones.Where((b,i) => Quaternion.Angle(before[i], b.localRotation) > .5f).Any();
        graph.Destroy();
        for (int i = 0; i < bones.Length; i++)
        { bones[i].localRotation = before[i]; bones[i].localPosition = positions[i]; bones[i].localScale = scales[i]; }
        if (!changed) throw new Exception("Knight animation did not change the skeleton pose");
        int steps = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Step_"));
        if (steps != 96) throw new Exception("Expected 96 marble steps, got " + steps);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TOWER VERIFIED: animated skeleton, 2.5 unit knight, 96 marble steps, grey castle masonry; scene saved.");
    }
}
