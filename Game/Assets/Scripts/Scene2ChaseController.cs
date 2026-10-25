using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Scene2ChaseController : MonoBehaviour
{
    public static Scene2ChaseController Instance { get; private set; }
    [SerializeField] private WitchBossAI witch;
    [SerializeField] private Transform frogLorePoint;
    [SerializeField] private float respawnProtection = 2.5f;

    [SerializeField] private string nextSceneName = "Scene3_TowerEscape";
    private float transitionFade;

    private PlayerController25D player;
    private bool reading;
    private readonly JournalBookView journal = new JournalBookView("Frog");
    private bool completed;
    private bool ownsPause;
    private float previousTimeScale;
    private float protectedUntil;
    private int restartCount;
    private string notice;
    private float noticeUntil;

    private void Awake() { Instance = this; }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController25D>();
        if (player != null) player.Respawned += OnPlayerRespawned;
        protectedUntil = Time.time + respawnProtection;
    }

    private bool NearFrog => player != null && frogLorePoint != null &&
        Mathf.Abs(player.transform.position.x - frogLorePoint.position.x) < 3f &&
        Mathf.Abs(player.transform.position.y - frogLorePoint.position.y) < 2.5f;

    private void Update()
    {
        if (LevelIntroCutscene.IsPlaying) return;
        if (player == null) return;
        if ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame))
        {
            StopAllCoroutines();
            RestoreTime();
            SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
            return;
        }
        if (completed || LevelIntroCutscene.IsPlaying) return;
        if (NearFrog && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (!reading && JournalBookView.Clicked(frogLorePoint))))
        {
            reading = !reading;
            player.isInteracting = reading;
            if (reading) { journal.Open(); PauseTime(); } else RestoreTime();
        }
        if (reading && (!NearFrog || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
        {
            reading = false;
            player.isInteracting = false;
            RestoreTime();
        }
    }

    public void HitPlayer(PlayerController25D victim, string cause = "Zehirlendin!")
    {
        if (victim != player || reading || completed || LevelIntroCutscene.IsPlaying || Time.time < protectedUntil) return;
        notice = cause + " Başlangıca döndün.";
        noticeUntil = Time.unscaledTime + 2.5f;
        player.Respawn();
    }

    private void OnPlayerRespawned()
    {
        restartCount++;
        reading = false;
        RestoreTime();
        protectedUntil = Time.time + respawnProtection;
        ClearHazards();
        if (witch != null) witch.ResetChase();
    }

    public void CompleteLevel()
    {
        if (completed || player == null) return;
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"Tower scene '{nextSceneName}' is missing from the build scene list.", this);
            return;
        }
        completed = true;
        reading = false;
        player.isInteracting = true;
        ClearHazards();
        if (witch != null) witch.StopChase();
        RestoreTime();
        StartCoroutine(EnterTower());
    }

    private IEnumerator EnterTower()
    {
        while (transitionFade < 1f)
        {
            transitionFade = Mathf.Min(1f, transitionFade + Time.unscaledDeltaTime / 0.8f);
            yield return null;
        }
        yield return SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
    }

    private void ClearHazards()
    {
        foreach (PoisonApple apple in FindObjectsByType<PoisonApple>(FindObjectsSortMode.None))
        { apple.gameObject.SetActive(false); Destroy(apple.gameObject); }
        foreach (PoisonCloud cloud in FindObjectsByType<PoisonCloud>(FindObjectsSortMode.None))
        { cloud.gameObject.SetActive(false); Destroy(cloud.gameObject); }
        foreach (DwarfAxeProjectile axe in FindObjectsByType<DwarfAxeProjectile>(FindObjectsSortMode.None))
        { axe.gameObject.SetActive(false); Destroy(axe.gameObject); }
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
        if (player != null)
        {
            player.Respawned -= OnPlayerRespawned;
            if (reading || completed) player.isInteracting = false;
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
        if (player == null || LevelIntroCutscene.IsPlaying) return;
        Matrix4x4 previous = StorybookUI.Begin();
        StorybookUI.Chapter("II", "Cadının Ormanı");
        StorybookUI.Stats($"Yeniden doğuş: {restartCount}");
        StorybookUI.Controls();
        if (Time.unscaledTime < noticeUntil) StorybookUI.Notice(notice);
        else if (Time.time < protectedUntil && !completed) StorybookUI.Notice("Kısa süreli korunma aktif. Yola devam et!");
        if (completed) StorybookUI.Dialog("Orman geride kalıyor…", "Kulenin kapısı açılıyor. Hikâyenin bir sonraki sayfası seni bekliyor.");
        else if (NearFrog) StorybookUI.Prompt("Kurbağanın Ardında Kalan Hikâye");
        if (transitionFade > 0f)
        {
            Color color = GUI.color;
            GUI.color = new Color(0, 0, 0, transitionFade);
            GUI.DrawTexture(new Rect(-1280, -720, 3840, 2160), Texture2D.whiteTexture);
            GUI.color = color;
        }
        GUI.matrix = previous;
    }
}
