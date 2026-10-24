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
    private bool completed;
    private bool ownsPause;
    private float previousTimeScale;
    private float protectedUntil;
    private int checkpointIndex;
    private float checkpointHeight;
    private int restartCount;
    private string notice;
    private float noticeUntil;
    private GUIStyle labelStyle;
    private GUIStyle headingStyle;

    public bool IsPlaying => !intro && !reading && !completed && Time.timeScale > 0f;
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
        if (player == null || Keyboard.current == null) return;
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestoreTime();
            SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
            return;
        }
        if (intro)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                BeginChase();
            return;
        }
        if (completed) return;
        if (IsPlaying && player.transform.position.y < checkpointHeight - 6f)
        { player.Respawn(); return; }
        if (NearShoes && Keyboard.current.eKey.wasPressedThisFrame)
        {
            reading = !reading;
            player.isInteracting = reading;
            if (reading) PauseTime(); else RestoreTime();
        }
        if (reading && (!NearShoes || Keyboard.current.escapeKey.wasPressedThisFrame))
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

    private void OnGUI()
    {
        if (player == null) return;
        if (intro && introCutscene != null && introCutscene.playableAsset != null)
        {
            GUI.Label(new Rect(24, Screen.height - 42, 350, 30), "E / Space / Esc: Cutscene'i geç");
            return;
        }
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            labelStyle.normal.textColor = Color.white;
            headingStyle = new GUIStyle(labelStyle) { fontSize = 25, fontStyle = FontStyle.Bold };
        }
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        GUI.Box(new Rect(20, 20, 760, 115), GUIContent.none);
        GUI.Label(new Rect(35, 28, 730, 35), "3. Bölüm — Kulenin İçinde", headingStyle);
        GUI.Label(new Rect(35, 68, 730, 60),
            "A/D: Hareket   Shift: Koş   Space: Zıpla / Çift zıpla   R: Yeniden başla\nMavi: Prens   Pembe kalpler: Öpücük   Yeşil: Kontrol noktası", labelStyle);
        float height = Mathf.Clamp(player.transform.position.y - 1f, 0f, topFloorHeight);
        GUI.Label(new Rect(1000, 30, 260, 70), $"Yükseklik: {height:0.0} / {topFloorHeight:0.0}\nTekrar: {restartCount}", labelStyle);
        if (intro && introCutscene != null && introCutscene.playableAsset != null)
        {
            GUI.Label(new Rect(35, 640, 730, 40), "E / Space / Esc: Cutscene'i geç", labelStyle);
        }
        else if (intro)
        {
            GUI.Box(new Rect(230, 230, 820, 230), GUIContent.none);
            GUI.Label(new Rect(260, 255, 760, 50), "Kuleye girdin... Prens yaklaşıyor.", headingStyle);
            GUI.Label(new Rect(260, 320, 760, 100),
                "Kule içi cutscene burada oynatılacak.\nŞimdilik E veya Space ile kaçışa başla.\nMerdivenlerden yüksel; boşlukları ve öpücükleri atlayarak aş.", labelStyle);
        }
        else if (completed)
        {
            GUI.Box(new Rect(300, 260, 680, 190), GUIContent.none);
            GUI.Label(new Rect(330, 285, 620, 50), "Kulenin tepesine ulaştın!", headingStyle);
            GUI.Label(new Rect(330, 345, 620, 70), "Prensten ve öpücüklerinden kurtuldun.\nTekrar oynamak için R.", labelStyle);
        }
        else if (reading)
        {
            GUI.Box(new Rect(220, 250, 840, 240), GUIContent.none);
            GUI.Label(new Rect(250, 275, 780, 45), "Kırılmış Cam Ayakkabılar", headingStyle);
            GUI.Label(new Rect(250, 330, 780, 110),
                "Kırık cam parçaları Sindirella'nın buradan geçtiğini söylüyor. " +
                "Bu kulede kurtarılmayı beklemek, masallardaki gibi mutlu bir son getirmemiş.\n\nE veya Esc ile devam et.", labelStyle);
        }
        else
        {
            if (NearShoes)
                GUI.Label(new Rect(420, 625, 600, 45), "[E] Kırık cam ayakkabıları incele", headingStyle);
            if (Time.time < protectedUntil)
                GUI.Label(new Rect(35, 145, 730, 35), "Kısa süreli korunma aktif!", labelStyle);
        }
        if (Time.unscaledTime < noticeUntil)
            GUI.Label(new Rect(240, 175, 900, 60), notice, labelStyle);
        GUI.matrix = oldMatrix;
    }
}
