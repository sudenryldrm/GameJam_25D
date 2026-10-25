using System.Collections.Generic;
using UnityEngine;

// Separate, capped UI and gameplay voices leave room for the future ambience track.
public sealed class StorySfx : MonoBehaviour
{
    private static StorySfx instance;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private readonly AudioSource[] ui = new AudioSource[4], world = new AudioSource[6];
    private bool paused;
    private float nextVoice;
    private float nextPlayerVoice;
    public static float Volume { get; set; } = .7f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; Volume = .7f; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance) return;
        var host = new GameObject("Story Sound Effects"); DontDestroyOnLoad(host);
        instance = host.AddComponent<StorySfx>();
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Audio/SFX")) instance.clips[clip.name] = clip;
        for (int i = 0; i < instance.ui.Length; i++) instance.ui[i] = instance.Voice(true);
        for (int i = 0; i < instance.world.Length; i++) instance.world[i] = instance.Voice(false);
    }

    private AudioSource Voice(bool menu)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
        source.ignoreListenerPause = menu; source.priority = menu ? 80 : 150;
        return source;
    }

    public static void Play(string id, float volume = .5f, bool menu = true, float pitch = 1)
    {
        if (!instance || (!menu && Time.timeScale <= 0)) return;
        if (id == "BookOpen") id = "BookMotion";
        else if (id == "BookClose") id = "BookMotion";
        if (!instance.clips.TryGetValue(id, out AudioClip clip)) return;
        float now = Time.unscaledTime;
        float cooldown = id == "Hurt" ? .35f : .07f;
        if (instance.lastPlayed.TryGetValue(id, out float last) && now - last < cooldown) return;
        instance.lastPlayed[id] = now;
        AudioSource[] pool = menu ? instance.ui : instance.world;
        AudioSource voice = null;
        foreach (AudioSource candidate in pool) if (!candidate.isPlaying) { voice = candidate; break; }
        // Skip a crowded footstep/impact rather than accumulating unbounded one-shots.
        if (!voice) return;
        voice.clip = clip; voice.volume = Mathf.Clamp01(volume * Volume);
        voice.pitch = Mathf.Clamp(pitch, .7f, 1.3f); voice.Play();
    }

    public static void Step(bool stone, bool wood)
    {
        string surface = stone ? "Stone" : wood ? "Wood" : "Grass";
        Play("Step" + surface + Random.Range(0, 3), stone ? .28f : .23f, false, Random.Range(.94f, 1.06f));
    }

    public static void Reaction(string actor, bool cinematic = false)
    {
        if (!instance || Time.unscaledTime < instance.nextVoice) return;
        if (!cinematic && (Time.timeScale <= 0 || LevelIntroCutscene.IsPlaying || StorybookCoverScreen.IsShowing)) return;
        string clip = actor == "Witch" ? "WitchLaugh" : actor + Random.Range(0, 2);
        if (cinematic) foreach (AudioSource source in instance.ui) if (source.clip &&
            (source.clip.name.StartsWith("Dwarf") || source.clip.name.StartsWith("Knight") || source.clip.name.StartsWith("Princess") || source.clip.name == "WitchLaugh")) source.Stop();
        Play(clip, actor == "Witch" ? .40f : .42f, cinematic, actor == "Dwarf" ? .88f : 1);
        instance.nextVoice = Time.unscaledTime + (actor == "Witch" ? 3.5f : 1.2f);
    }

    public static void StopReactions()
    {
        if (!instance) return;
        foreach (AudioSource source in instance.ui) if (source.clip &&
            (source.clip.name.StartsWith("Dwarf") || source.clip.name.StartsWith("Knight") || source.clip.name.StartsWith("Princess") || source.clip.name == "WitchLaugh")) source.Stop();
        instance.nextVoice = 0;
    }

    public static void PrincessEffort(bool hurt = false)
    {
        if (!instance || (!hurt && Time.unscaledTime < instance.nextPlayerVoice)) return;
        instance.nextPlayerVoice = Time.unscaledTime + .55f;
        Play(hurt ? "PrincessHurt" : "PrincessJump" + Random.Range(0, 2), hurt ? .48f : .30f, hurt);
    }

    private void Update()
    {
        bool next = Time.timeScale <= 0;
        if (next && !paused) foreach (AudioSource source in world) source.Stop();
        paused = next;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
