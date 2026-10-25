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
    private GUIStyle textStyle;
    private GUIStyle headingStyle;

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
        if (reading) { journal.Draw(CloseJournal); return; }
        if (player == null || LevelIntroCutscene.IsPlaying) return;
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            textStyle.normal.textColor = Color.white;
            headingStyle = new GUIStyle(textStyle) { fontSize = 25, fontStyle = FontStyle.Bold };
        }
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        GUI.Box(new Rect(20, 20, 680, 110), GUIContent.none);
        GUI.Label(new Rect(35, 28, 650, 30), "2. Bölüm — Cadıdan Kaçış", headingStyle);
        GUI.Label(new Rect(35, 65, 650, 55),
            "A/D: Hareket   Shift: Koş   Space: Zıpla / Çift zıpla   R: Yeniden başla\nMor: Cadı   Kırmızı: Zehirli elma   Turuncu: Cüce baltası   Yeşil: Zehir bulutu", textStyle);
        GUI.Label(new Rect(1030, 25, 230, 35), $"Tekrar: {restartCount}", textStyle);
        if (Time.time < protectedUntil && !completed)
            GUI.Label(new Rect(35, 140, 750, 35), "Kısa süreli korunma aktif — ilerle!", textStyle);
        if (Time.unscaledTime < noticeUntil)
            GUI.Label(new Rect(350, 170, 650, 40), notice, headingStyle);
        if (completed)
        {
            GUI.Box(new Rect(310, 260, 660, 170), GUIContent.none);
            GUI.Label(new Rect(340, 285, 600, 50), "Kuleye giriyorsun...", headingStyle);
            GUI.Label(new Rect(340, 340, 600, 65),
                "Kulenin kapısı açılıyor.", textStyle);
        }
        else if (NearFrog)
        {
            GUI.Box(new Rect(420, 610, 440, 55), GUIContent.none);
            GUI.Label(new Rect(440, 620, 400, 40), "[E / Tıkla] Ölü kurbağayı incele", textStyle);
        }
        if (transitionFade > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, transitionFade);
            GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
        GUI.matrix = previous;
    }
}
