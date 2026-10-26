using UnityEngine;
using UnityEngine.SceneManagement;

// Shared dialogue and chase loops for all three chapters; the opening tour remains separate.
public sealed class FirstLevelMusic : MonoBehaviour
{
    private static FirstLevelMusic instance;
    private AudioSource conversation, chase, cover;
    private bool coverPlaying;
    private int mode;
    public static float Volume { get; set; } = .9f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; Volume = .9f; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance) return;
        var host = new GameObject("Story Conversation and Chase Music"); DontDestroyOnLoad(host);
        instance = host.AddComponent<FirstLevelMusic>();
        instance.conversation = instance.Create("ConversationLoop");
        instance.chase = instance.Create("ChaseLoop");
        instance.cover = instance.Create("CoverLoop");
        SceneManager.activeSceneChanged += instance.SceneChanged;
    }

    private AudioSource Create(string name)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/Music/" + name);
        source.loop = true; source.playOnAwake = false; source.spatialBlend = 0;
        source.ignoreListenerPause = true; source.volume = 0; source.priority = 200;
        if (!source.clip) Debug.LogError("Story music missing: " + name);
        return source;
    }

    public static void Conversation() { if (instance) instance.Switch(1); }
    public static void Chase() { if (instance) instance.Switch(2); }
    public static void Cover(bool playing) { if (!instance) return; instance.coverPlaying = playing; if (playing && instance.cover.clip && !instance.cover.isPlaying) { instance.cover.volume = 0; instance.cover.Play(); } }

    public static void Stop() { if (instance) instance.mode = 0; }

    private void Switch(int next)
    {
        string scene = SceneManager.GetActiveScene().name;
        if ((scene != "SampleScene" && scene != "Scene2_WitchChase" && scene != "Scene3_TowerEscape") || mode == next) return;
        mode = next;
        AudioSource source = next == 1 ? conversation : chase;
        if (source.clip && !source.isPlaying) { source.time = 0; source.Play(); }
    }

    private void Update()
    {
        // Keep dialogue audible while its cutscene pauses physics; soften chase music while reading journals.
        float duck = mode == 2 && Time.timeScale <= 0 ? .55f : 1;
        float feedbackDuck = StorySfx.MusicDuck;
        Fade(conversation, mode == 1 ? Volume * .70f * feedbackDuck : 0);
        Fade(chase, mode == 2 ? Volume * .88f * duck * feedbackDuck : 0);
        float coverTarget = coverPlaying ? Volume * .88f * feedbackDuck : 0;
        cover.volume = Mathf.MoveTowards(cover.volume, coverTarget, Time.unscaledDeltaTime * .62f);
        if (!coverPlaying && cover.volume <= .0001f && cover.isPlaying) cover.Stop();
    }

    private void Fade(AudioSource source, float target)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * .85f);
        if (target == 0 && source.volume <= .0001f && source.isPlaying) source.Stop();
    }

    private void SceneChanged(Scene previous, Scene next)
    {
        mode = 0; coverPlaying = false; cover.Stop(); cover.volume = 0;
        conversation.Stop(); chase.Stop(); conversation.volume = chase.volume = 0;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
        if (instance == this) instance = null;
    }
}
