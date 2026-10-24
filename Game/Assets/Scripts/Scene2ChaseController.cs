using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Scene2ChaseController : MonoBehaviour
{
    public static Scene2ChaseController Instance { get; private set; }
    [SerializeField] private WitchBossAI witch;
    [SerializeField] private Transform frogLorePoint;
    [SerializeField] private float respawnProtection = 2.5f;

    private PlayerController25D player;
    private bool reading;
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
        if (Keyboard.current == null || player == null) return;
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestoreTime();
            SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
            return;
        }
        if (completed) return;
        if (NearFrog && Keyboard.current.eKey.wasPressedThisFrame)
        {
            reading = !reading;
            player.isInteracting = reading;
            if (reading) PauseTime(); else RestoreTime();
        }
        if (reading && (!NearFrog || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            reading = false;
            player.isInteracting = false;
            RestoreTime();
        }
    }

    public void HitPlayer(PlayerController25D victim)
    {
        if (victim != player || reading || completed || Time.time < protectedUntil) return;
        notice = "Zehirlendin! Başlangıca döndün.";
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
        completed = true;
        reading = false;
        player.isInteracting = true;
        ClearHazards();
        if (witch != null) witch.StopChase();
        PauseTime();
    }

    private void ClearHazards()
    {
        foreach (PoisonApple apple in FindObjectsByType<PoisonApple>(FindObjectsSortMode.None))
        { apple.gameObject.SetActive(false); Destroy(apple.gameObject); }
        foreach (PoisonCloud cloud in FindObjectsByType<PoisonCloud>(FindObjectsSortMode.None))
        { cloud.gameObject.SetActive(false); Destroy(cloud.gameObject); }
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

    private void OnGUI()
    {
        if (player == null) return;
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
            "A/D: Hareket   Shift: Koş   Space: Zıpla / Çift zıpla   R: Yeniden başla\nMor: Cadı   Kırmızı: Zehirli elma   Yeşil: Zehir bulutu", textStyle);
        GUI.Label(new Rect(1030, 25, 230, 35), $"Tekrar: {restartCount}", textStyle);
        if (Time.time < protectedUntil && !completed)
            GUI.Label(new Rect(35, 140, 750, 35), "Kısa süreli korunma aktif — ilerle!", textStyle);
        if (Time.unscaledTime < noticeUntil)
            GUI.Label(new Rect(350, 170, 650, 40), notice, headingStyle);
        if (completed)
        {
            GUI.Box(new Rect(310, 260, 660, 170), GUIContent.none);
            GUI.Label(new Rect(340, 285, 600, 50), "Cadıdan kurtuldun!", headingStyle);
            GUI.Label(new Rect(340, 340, 600, 65),
                "2. bölümün sonuna ulaştın.\nTekrar denemek için R'ye bas.", textStyle);
        }
        else if (reading)
        {
            GUI.Box(new Rect(220, 250, 840, 240), GUIContent.none);
            GUI.Label(new Rect(250, 275, 780, 45), "Ölü Kurbağa", headingStyle);
            GUI.Label(new Rect(250, 330, 780, 110),
                "Bir zamanlar bir prens olduğuna inanılan bu kurbağa, cadının zehirli deneylerinden sağ çıkamamış. " +
                "Yerdeki elma parçaları, sıradaki kurbanın kim olacağını fısıldıyor.\n\nDevam etmek için E veya Esc.", textStyle);
        }
        else if (NearFrog)
        {
            GUI.Box(new Rect(420, 610, 440, 55), GUIContent.none);
            GUI.Label(new Rect(440, 620, 400, 40), "[E] Ölü kurbağayı incele", textStyle);
        }
        GUI.matrix = previous;
    }
}
