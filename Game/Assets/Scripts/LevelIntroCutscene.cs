using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Unity.Cinemachine;

[DefaultExecutionOrder(500)]
public sealed class LevelIntroCutscene : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool useStoryDialogue = true;
    public static bool IsPlaying { get; private set; }
    private PlayerController25D player;
    private Transform boss;
    private Rigidbody bossBody;
    private ConversationActorPose playerPose, bossPose;
    private MapOpeningFlythrough opening;
    private bool dialogueStarted;
    private CameraFollow25D cameraFollow;
    private CinemachineBrain brain;
    private bool previousIgnoreTime, previousInteraction, running, dialogue, exiting, revealAll;
    private float previousTimeScale, beganAt, lineBeganAt, exitBeganAt, exitZoom;
    private int line, lastAdvanceFrame = -1;
    private string[] lines;
    private string bossName;
    private Vector3 bossPosition, stagedBossPosition;
    private Quaternion bossRotation, playerRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { IsPlaying = false; }
    private void Start()
    {
        if (!playOnStart) return;
        if (gameObject.scene.name == "SampleScene" && StorybookCoverScreen.NeedsStart)
            (GetComponent<StorybookCoverScreen>() ?? gameObject.AddComponent<StorybookCoverScreen>()).ShowStart(StartGameOpening);
        else PlayIntro();
    }

    public void PlayIntro()
    {
        if (running || IsPlaying) return;
        player = FindFirstObjectByType<PlayerController25D>();
        if (!player) return;
        dialogue = useStoryDialogue && FindConversation();
        if (!dialogue && (!director || !director.playableAsset)) return;
        previousInteraction = player.isInteracting;
        previousTimeScale = Time.timeScale;
        player.isInteracting = true;
        if (player.TryGetComponent(out Rigidbody body))
        { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        running = true; IsPlaying = true; Time.timeScale = 0;
        if (!dialogue)
        {
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.extrapolationMode = DirectorWrapMode.None;
            director.stopped += OnStopped;
            director.time = 0; director.Play();
            return;
        }
        bossBody = boss.GetComponent<Rigidbody>();
        bossPosition = boss.position; bossRotation = boss.rotation; playerRotation = player.transform.rotation;
        playerPose = new ConversationActorPose(player.transform);
        bossPose = new ConversationActorPose(boss);
        Physics.SyncTransforms();
        playerPose.Ground();
        Vector3 start = player.transform.position;
        float bossX = gameObject.scene.name == "Scene3_TowerEscape" ? Mathf.Max(-10.4f, start.x - 2f) : start.x - 2.8f;
        SetBoss(new Vector3(bossX, start.y, start.z), bossRotation);
        Physics.SyncTransforms();
        bossPose.Ground(bossName == "Cadı" ? .15f : 0);
        stagedBossPosition = boss.position;
        playerPose.Face(boss.position); bossPose.Face(player.transform.position);
        cameraFollow = FindFirstObjectByType<CameraFollow25D>();
        if (cameraFollow) cameraFollow.BeginCameraSequence();
        Camera camera = Camera.main;
        brain = camera ? camera.GetComponent<CinemachineBrain>() : null;
        if (brain) { previousIgnoreTime = brain.IgnoreTimeScale; brain.IgnoreTimeScale = true; }
        dialogueStarted = false;
        opening = GetComponent<MapOpeningFlythrough>() ?? gameObject.AddComponent<MapOpeningFlythrough>();
        if (!opening.TryPlay(cameraFollow, BeginDialogueZoom)) BeginDialogueZoom();
    }

    // Future Start buttons can call this without needing a separate opening implementation.
    public void StartGameOpening()
    {
        if (running) return;
        MapOpeningFlythrough.ResetForNewGame();
        PlayIntro();
    }

    private void BeginDialogueZoom()
    {
        if (!running) return;
        dialogueStarted = true;
        if (cameraFollow) cameraFollow.BeginConversationShot((player.transform.position + stagedBossPosition) * .5f);
        beganAt = Time.unscaledTime; lineBeganAt = beganAt + .75f;
        line = 0; exiting = false; revealAll = false;
    }

    private void LateUpdate()
    {
        if (!running || !dialogue || exiting || !player || !boss) return;
        playerPose.Face(boss.position);
        bossPose.Face(player.transform.position);
    }

    private bool FindConversation()
    {
        string scene = gameObject.scene.name;
        if (scene == "SampleScene")
        {
            float nearest = float.PositiveInfinity;
            foreach (DwarfPursuer dwarf in FindObjectsByType<DwarfPursuer>(FindObjectsSortMode.None))
            {
                float distance = (dwarf.transform.position - player.transform.position).sqrMagnitude;
                if (dwarf.gameObject.scene != gameObject.scene || distance >= nearest) continue;
                nearest = distance; boss = dwarf.transform;
            }
            bossName = "Öndeki Cüce";
            lines = new[] {
                "Ormana tek başına çıkma prenses. Bizimle kal; burada güvende olursun.",
                "Peki gitmek istersem?",
                "Neden isteyesin? Sen evimizi çekip çevirirsin, biz de seni koruruz. Hepimiz mutlu yaşarız.",
                "Hepimiz mi, yoksa yalnızca siz mi?",
                "Masal böyle devam eder. Yerini bırakıp gidemezsin.",
                "O zaman bu masal bensiz devam etsin.",
                "Dur! Geri dön!" };
        }
        else if (scene == "Scene2_WitchChase")
        {
            WitchBossAI witch = FindFirstObjectByType<WitchBossAI>(); boss = witch ? witch.transform : null;
            bossName = "Cadı";
            lines = new[] {
                "Çok yorulmuşsun güzel kızım. Al, şu elmadan bir ısırık. Kendine gelirsin.",
                "Önce sen ye.",
                "Benim ihtiyacım yok. Bu elmayı özellikle senin için getirdim.",
                "Ne kadar düşüncelisin… Ama istemiyorum.",
                "Bir ısırık alacaksın. Bunca yolu boşuna gelmedim!",
                "Demek hediyeyi reddetme hakkım yokmuş.",
                "O elmayı yemeden buradan gidemezsin!",
                "Bakalım yetişebilecek misin?" };
        }
        else if (scene == "Scene3_TowerEscape")
        {
            PrinceChaserAI prince = FindFirstObjectByType<PrinceChaserAI>(); boss = prince ? prince.transform : null;
            bossName = "Prens";
            lines = new[] {
                "Sonunda seni buldum. Artık korkmana gerek yok; seni kurtarmaya geldim.",
                "Güzel. Çıkış nerede?",
                "Acele etme. Önce bir öpücük… Sonra burada, benimle kalacaksın.",
                "Ben senden bunların hiçbirini istemedim.",
                "Ama masalın sonu bu. Prens, prensesi bulur ve sonsuza dek mutlu yaşarlar.",
                "Benim mutlu olup olmayacağıma ne zaman karar verdin?",
                "Nereye gidiyorsun? Daha mutlu sonumuza ulaşmadık!",
                "Senin sonun olabilir. Ben kendi yolumu bulacağım." };
        }
        return boss && lines != null && lines.Length > 0;
    }

    private void Update()
    {
        if (!running || (opening && opening.Running)) return;
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool advance = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        advance |= Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (!dialogue) { if (escape || advance) Finish(); return; }
        if (exiting)
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - exitBeganAt) / .65f));
            if (cameraFollow) cameraFollow.BlendConversationShot(exitZoom * (1 - t));
            SetBoss(Vector3.Lerp(stagedBossPosition, bossPosition, t), Quaternion.Slerp(boss.rotation, bossRotation, t));
            playerPose.Set(player.transform.position, Quaternion.Slerp(player.transform.rotation, playerRotation, t));
            if (t >= 1) Finish();
            return;
        }
        float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - beganAt) / .75f));
        if (cameraFollow) cameraFollow.BlendConversationShot(blend);
        if (escape) { BeginExit(); return; }
        if (advance && Time.unscaledTime >= lineBeganAt) Advance();
    }

    private int VisibleCharacters => revealAll ? lines[line].Length :
        Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - lineBeganAt) * 42), 0, lines[line].Length);

    private void Advance()
    {
        if (exiting || lastAdvanceFrame == Time.frameCount || Time.unscaledTime < lineBeganAt) return;
        lastAdvanceFrame = Time.frameCount;
        if (VisibleCharacters < lines[line].Length) { revealAll = true; return; }
        if (++line >= lines.Length) { line = lines.Length - 1; BeginExit(); return; }
        lineBeganAt = Time.unscaledTime; revealAll = false;
    }

    private void BeginExit()
    {
        if (exiting) return;
        exiting = true; exitBeganAt = Time.unscaledTime;
        exitZoom = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - beganAt) / .75f));
    }
    private void OnStopped(PlayableDirector stoppedDirector) { Finish(); }
    private void SetBoss(Vector3 position, Quaternion rotation)
    {
        if (!boss) return;
        if (bossBody) { bossBody.position = position; bossBody.rotation = rotation; }
        boss.SetPositionAndRotation(position, rotation);
    }

    private void Finish()
    {
        if (!running) return;
        running = false; IsPlaying = false;
        if (opening && opening.Running) opening.Cancel();
        if (dialogue)
        {
            SetBoss(bossPosition, bossRotation);
            if (player) playerPose.Set(player.transform.position, playerRotation);
            playerPose?.Release(); bossPose?.Release();
            if (cameraFollow) cameraFollow.EndConversationShot();
            if (brain) brain.IgnoreTimeScale = previousIgnoreTime;
        }
        else if (director) { director.stopped -= OnStopped; director.Stop(); }
        Time.timeScale = previousTimeScale;
        if (player) player.isInteracting = previousInteraction;
    }

    private void OnDisable() { Finish(); }
    private void OnGUI()
    {
        if (!running || (opening && opening.Running)) return;
        if (!dialogue) { StorybookUI.SkipPrompt(); return; }
        if (!dialogueStarted) return;
        if (exiting || Time.unscaledTime < lineBeganAt) return;
        Matrix4x4 previous = StorybookUI.Begin();
        string text = lines[line].Substring(0, VisibleCharacters);
        if (StorybookUI.Conversation(line % 2 == 0 ? bossName : "Prenses", text, line + 1, lines.Length)) Advance();
        GUI.matrix = previous;
    }
}
