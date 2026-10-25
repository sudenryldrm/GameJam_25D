using UnityEngine;
using UnityEngine.SceneManagement;

// Dialogue and chase have independent looping sources; the opening tour remains separate.
public sealed class FirstLevelMusic : MonoBehaviour
{
    private static FirstLevelMusic instance;
    private AudioSource conversation, chase;
    private int mode;
    public static float Volume { get; set; } = .6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; Volume = .6f; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance) return;
        var host = new GameObject("First Chapter Music"); DontDestroyOnLoad(host);
        instance = host.AddComponent<FirstLevelMusic>();
        instance.conversation = instance.Create("DwarfConversationLoop");
        instance.chase = instance.Create("DwarfChaseLoop");
        SceneManager.activeSceneChanged += instance.SceneChanged;
    }

    private AudioSource Create(string name)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/Music/" + name);
        source.loop = true; source.playOnAwake = false; source.spatialBlend = 0;
        source.ignoreListenerPause = true; source.volume = 0; source.priority = 200;
        if (!source.clip) Debug.LogError("First chapter music missing: " + name);
        return source;
    }

    public static void Conversation() { if (instance) instance.Switch(1); }
    public static void Chase() { if (instance) instance.Switch(2); }

    private void Switch(int next)
    {
        if (SceneManager.GetActiveScene().name != "SampleScene" || mode == next) return;
        mode = next;
        AudioSource source = next == 1 ? conversation : chase;
        if (source.clip && !source.isPlaying) { source.time = 0; source.Play(); }
    }

    private void Update()
    {
        // Keep dialogue audible while its cutscene pauses physics; soften chase music while reading journals.
        float duck = mode == 2 && Time.timeScale <= 0 ? .28f : 1;
        Fade(conversation, mode == 1 ? Volume * .48f : 0);
        Fade(chase, mode == 2 ? Volume * .58f * duck : 0);
    }

    private void Fade(AudioSource source, float target)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * .48f);
        if (target == 0 && source.volume <= .0001f && source.isPlaying) source.Stop();
    }

    private void SceneChanged(Scene previous, Scene next)
    {
        mode = 0;
        conversation.Stop(); chase.Stop(); conversation.volume = chase.volume = 0;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
        if (instance == this) instance = null;
    }
}
