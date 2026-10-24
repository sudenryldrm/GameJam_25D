using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

// Plays the first level intro, then restores normal gameplay.
public sealed class LevelIntroCutscene : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private bool playOnStart = true;
    public static bool IsPlaying { get; private set; }

    private PlayerController25D player;
    private bool running;
    private bool previousInteraction;
    private float previousTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { IsPlaying = false; }

    private void Start()
    {
        if (playOnStart) PlayIntro();
    }

    public void PlayIntro()
    {
        if (running || IsPlaying || director == null || director.playableAsset == null) return;
        player = FindFirstObjectByType<PlayerController25D>();
        if (player == null) return;
        previousInteraction = player.isInteracting;
        previousTimeScale = Time.timeScale;
        player.isInteracting = true;
        if (player.TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        running = true;
        IsPlaying = true;
        Time.timeScale = 0f;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        director.extrapolationMode = DirectorWrapMode.None;
        director.stopped += OnStopped;
        director.time = 0;
        director.Play();
    }

    private void Update()
    {
        if (!running || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
            Finish();
    }

    private void OnStopped(PlayableDirector stoppedDirector) { Finish(); }

    private void Finish()
    {
        if (!running) return;
        running = false;
        IsPlaying = false;
        if (director != null)
        {
            director.stopped -= OnStopped;
            director.Stop();
        }
        Time.timeScale = previousTimeScale;
        if (player != null) player.isInteracting = previousInteraction;
    }

    private void OnDisable() { Finish(); }

    private void OnGUI()
    {
        if (!running) return;
        GUI.Label(new Rect(24, Screen.height - 42, 350, 30), "E / Space / Esc: Cutscene'i geç");
    }
}
