using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TowerLevelController : MonoBehaviour
{
    public static TowerLevelController Instance { get; private set; }
    [SerializeField] private PrinceChaserAI prince;
    [SerializeField] private Transform brokenShoes;
    [SerializeField] private PlayableDirector introCutscene;
    [SerializeField] private float topFloorHeight = 36.5f;
    [SerializeField] private float protectionDuration = 2.5f;

    private PlayerController25D player;
    private bool intro = true;
    private bool reading;
    private readonly JournalBookView journal = new JournalBookView("Shoes");
    private bool completed;
    private bool ownsPause;
    private float previousTimeScale;
    private float protectedUntil;
    private int checkpointIndex;
    private float checkpointHeight;
    private int restartCount;
    private string notice;
    private float noticeUntil;

    public bool IsPlaying => !LevelIntroCutscene.IsPlaying && !intro && !reading && !completed && Time.timeScale > 0f;
    private bool NearShoes => player != null && brokenShoes != null &&
        Vector3.Distance(player.transform.position, brokenShoes.position) < 2.5f;

    private void Awake() { Instance = this; }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController25D>();
        if (player == null) { Debug.LogError("Tower player is missing.", this); return; }
        player.Respawned += OnPlayerRespawned;
        checkpointHeight = player.transform.position.y;
        player.isInteracting = true;
        PauseTime();
        if (prince != null) prince.ResetChase(0);
        if (introCutscene != null && introCutscene.playableAsset != null)
        {
            introCutscene.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            introCutscene.stopped += OnIntroStopped;
            introCutscene.Play();
        }
        else
        {
            // Until this level gets its own cutscene, enter gameplay immediately.
            BeginChase();
        }
    }

    private void OnIntroStopped(PlayableDirector director) { BeginChase(); }

    private void BeginChase()
    {
        if (!intro || player == null) return;
        intro = false;
        if (introCutscene != null)
        {
            introCutscene.stopped -= OnIntroStopped;
            introCutscene.Stop();
        }
        player.isInteracting = false;
        RestoreTime();
        protectedUntil = Time.time + protectionDuration;
        if (prince != null) prince.ResetChase(checkpointIndex);
    }

    private void Update()
    {
        if (LevelIntroCutscene.IsPlaying) return;
        if (player == null) return;
        if ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame))
        {
            RestoreTime();
            SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
            return;
        }
        if (intro)
        {
            if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
                BeginChase();
            return;
        }
        if (completed) return;
        if (IsPlaying && player.transform.position.y < checkpointHeight - 6f)
        { player.Respawn(); return; }
        if (NearShoes && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (!reading && JournalBookView.Clicked(brokenShoes))))
        {
            reading = !reading;
            player.isInteracting = reading;
            if (reading) { journal.Open(); PauseTime(); } else RestoreTime();
        }
        if (reading && (!NearShoes || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
        {
            reading = false;
            player.isInteracting = false;
            RestoreTime();
        }
    }

    public void HitPlayer(PlayerController25D victim, string reason)
    {
        if (victim != player || !IsPlaying || Time.time < protectedUntil) return;
        notice = reason + " Son kontrol noktasına döndün.";
        noticeUntil = Time.unscaledTime + 2.5f;
        player.Respawn();
    }

    public void SetCheckpoint(Vector3 spawnPosition, int routeIndex)
    {
        if (!IsPlaying || routeIndex <= checkpointIndex || player == null) return;
        checkpointIndex = routeIndex;
        checkpointHeight = spawnPosition.y;
        player.SetCheckpoint(spawnPosition);
        notice = "Kontrol noktası kaydedildi.";
        noticeUntil = Time.unscaledTime + 2.5f;
    }

    private void OnPlayerRespawned()
    {
        restartCount++;
        reading = false;
        player.isInteracting = false;
        RestoreTime();
        protectedUntil = Time.time + protectionDuration;
        ClearKisses();
        if (prince != null) prince.ResetChase(checkpointIndex);
    }

    public void CompleteLevel()
    {
        if (!IsPlaying || player == null) return;
        completed = true;
        player.isInteracting = true;
        ClearKisses();
        if (prince != null) prince.StopChase();
        PauseTime();
        (GetComponent<StorybookCoverScreen>() ?? gameObject.AddComponent<StorybookCoverScreen>()).ShowEnding();
    }

    private void ClearKisses()
    {
        foreach (KissProjectile kiss in FindObjectsByType<KissProjectile>(FindObjectsSortMode.None))
        { kiss.gameObject.SetActive(false); Destroy(kiss.gameObject); }
    }

    private void PauseTime()
    {
        if (ownsPause) return;
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0f;
    }

    private void RestoreTime()
    {
        if (!ownsPause) return;
        Time.timeScale = previousTimeScale;
        ownsPause = false;
    }

    private void OnDisable()
    {
        RestoreTime();
        if (introCutscene != null) introCutscene.stopped -= OnIntroStopped;
        if (player != null)
        {
            player.Respawned -= OnPlayerRespawned;
            if (intro || reading || completed) player.isInteracting = false;
        }
        if (Instance == this) Instance = null;
    }

    private void CloseJournal()
    {
        reading = false;
        if (player != null) player.isInteracting = false;
        RestoreTime();
    }

    private void OnGUI()
    {
        if (LevelIntroCutscene.IsPlaying) return;
        if (reading) { journal.Draw(CloseJournal); return; }
        if (player == null) return;
        if (intro && introCutscene != null && introCutscene.playableAsset != null)
        { StorybookUI.SkipPrompt(); return; }
        Matrix4x4 previous = StorybookUI.Begin();
        StorybookUI.Chapter("III", "Ayışığı Kulesi");
        float height = Mathf.Clamp(player.transform.position.y - 1f, 0, topFloorHeight);
        StorybookUI.Stats($"Yükseliş: {height:0.0} / {topFloorHeight:0.0}\nYeniden doğuş: {restartCount}");
        StorybookUI.Controls();
        if (intro) StorybookUI.Dialog("Kule sessiz değil…", "Prens seni bulmadan merdivenlerden yüksel.\nE veya Space ile yola devam et.");
        else if (completed) StorybookUI.Dialog("Masalın sonunu sen yazdın.", "Kulenin tepesine ulaştın; prensten ve öpücüklerinden kurtuldun.\nR ile yeniden başla.");
        else if (NearShoes) StorybookUI.Prompt("Kırık Cam Ayakkabıların Hikâyesi");
        if (Time.unscaledTime < noticeUntil) StorybookUI.Notice(notice);
        else if (!intro && !completed && Time.time < protectedUntil) StorybookUI.Notice("Kısa süreli korunma aktif. Merdivenlerden yüksel!");
        GUI.matrix = previous;
    }
}
