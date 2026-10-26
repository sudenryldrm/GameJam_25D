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
    private float duckUntil, duckAmount = 1;
    private readonly List<AudioClip> preparedClips = new List<AudioClip>();
    private readonly HashSet<string> missing = new HashSet<string>();
    public static float MusicDuck => instance && Time.unscaledTime < instance.duckUntil ? instance.duckAmount : 1;
    public static float Volume { get; set; } = .95f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; Volume = .95f; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance) return;
        var host = new GameObject("Story Sound Effects"); DontDestroyOnLoad(host);
        instance = host.AddComponent<StorySfx>();
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Audio/SFX")) instance.clips[clip.name] = instance.PrepareClip(clip);
        for (int i = 0; i < instance.ui.Length; i++) instance.ui[i] = instance.Voice(true);
        for (int i = 0; i < instance.world.Length; i++) instance.world[i] = instance.Voice(false);
    }

    private AudioClip PrepareClip(AudioClip original)
    {
        var data = new float[original.samples * original.channels];
        if (!original.GetData(data, 0)) { Debug.LogWarning("Cannot balance SFX: " + original.name); return original; }
        double energy = 0;
        foreach (float value in data) energy += value * value;
        float rms = (float)System.Math.Sqrt(energy / Mathf.Max(1, data.Length));
        if (rms < .000001f) return original;
        float target = original.name.StartsWith("Step") ? .14f : .20f;
        float gain = Mathf.Clamp(target / rms, .25f, 64f);
        // A soft knee preserves quieter detail without digital clipping on louder transients.
        for (int i = 0; i < data.Length; i++)
        {
            float value = data[i] * gain, amplitude = Mathf.Abs(value);
            if (amplitude > .78f) value = Mathf.Sign(value) * (.78f + .16f * (float)System.Math.Tanh((amplitude - .78f) / .16f));
            data[i] = value;
        }
        var clip = AudioClip.Create(original.name, original.samples, original.channels, original.frequency, false);
        clip.SetData(data, 0); preparedClips.Add(clip); return clip;
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
        if (!instance.clips.TryGetValue(id, out AudioClip clip)) { if (instance.missing.Add(id)) Debug.LogWarning("Missing SFX: " + id); return; }
        float now = Time.unscaledTime;
        float cooldown = id == "Hurt" ? .35f : .07f;
        if (instance.lastPlayed.TryGetValue(id, out float last) && now - last < cooldown) return;
        instance.lastPlayed[id] = now;
        AudioSource[] pool = menu ? instance.ui : instance.world;
        AudioSource voice = null;
        foreach (AudioSource candidate in pool) if (!candidate.isPlaying) { voice = candidate; break; }
        bool step = id.StartsWith("Step");
        // Important feedback can replace a quieter/older voice instead of disappearing.
        if (!voice && !step)
        {
            foreach (AudioSource candidate in pool)
                if (!voice || candidate.volume < voice.volume) voice = candidate;
        }
        if (!voice) return;
        if (id == "BookMotion") volume = Mathf.Max(volume, .9f);
        else if (id == "Click") volume = Mathf.Max(volume, .72f);
        else if (id.StartsWith("Page")) volume = Mathf.Max(volume, .58f);
        else if (id == "Jump" || id == "DoubleJump" || id == "Land") volume = Mathf.Max(volume, .58f);
        else if (id.StartsWith("Princess") || id.StartsWith("Knight") || id.StartsWith("Dwarf") || id == "WitchLaugh") volume = Mathf.Max(volume, .55f);
        else if (step) volume = Mathf.Max(volume, .32f);
        voice.clip = clip; voice.volume = Mathf.Clamp01(volume * Volume);
        voice.pitch = Mathf.Clamp(pitch, .7f, 1.3f); voice.Play();
        if (!step)
        {
            float duck = id == "BookMotion" ? .42f : menu ? .62f : .75f;
            if (now >= instance.duckUntil) instance.duckAmount = duck;
            else instance.duckAmount = Mathf.Min(instance.duckAmount, duck);
            instance.duckUntil = Mathf.Max(instance.duckUntil, now + Mathf.Clamp(clip.length / voice.pitch, .2f, 1.4f));
        }
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

    private void OnDestroy() { foreach (AudioClip clip in preparedClips) if (clip) Destroy(clip); if (instance == this) instance = null; }
}
