using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Hareket Hızları")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Animasyon Dosyaları")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip jumpClip;

    [Header("Görsel ve Eğilme Ayarları")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float modelStandingY = -1f;      // Normal duruş ayak yüksekliği
    [SerializeField] private float modelCrouchY = -0.55f;     // Eğilince ayakların yere batmaması için yükseklik

    [Header("Zemin Kontrolü")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Animator anim;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpRequested;
    private bool isCrouching;
    private bool isRunning;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable runPlayable;
    private AnimationClipPlayable jumpPlayable;
    private bool hasAnimationSetup = false;

    private double idleTimer = 0.0;
    private double runTimer = 0.0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        originalColliderHeight = col.height;
        originalColliderCenter = col.center;

        if (visualModel == null && transform.childCount > 0)
        {
            visualModel = transform.GetChild(0);
        }

        if (visualModel != null)
        {
            anim = visualModel.GetComponent<Animator>();
            if (anim == null) anim = visualModel.gameObject.AddComponent<Animator>();
            anim.applyRootMotion = false;
        }

        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        SetupAnimations();
    }

    private void SetupAnimations()
    {
        if (anim == null || idleClip == null || runClip == null) return;

        playableGraph = PlayableGraph.Create("PlayerGraph");
        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", anim);

        int count = (jumpClip != null) ? 3 : 2;
        mixer = AnimationMixerPlayable.Create(playableGraph, count);
        playableOutput.SetSourcePlayable(mixer);

        idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
        runPlayable = AnimationClipPlayable.Create(playableGraph, runClip);

        idleClip.wrapMode = WrapMode.Loop;
        runClip.wrapMode = WrapMode.Loop;

        playableGraph.Connect(idlePlayable, 0, mixer, 0);
        playableGraph.Connect(runPlayable, 0, mixer, 1);

        if (jumpClip != null)
        {
            jumpPlayable = AnimationClipPlayable.Create(playableGraph, jumpClip);
            playableGraph.Connect(jumpPlayable, 0, mixer, 2);
        }

        mixer.SetInputWeight(0, 1f);
        playableGraph.Play();
        hasAnimationSetup = true;
    }

    private void Update()
    {
        horizontalInput = 0f;
        isRunning = false;
        isCrouching = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                horizontalInput = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                horizontalInput = 1f;

            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                isRunning = true;

            if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed)
            {
                isCrouching = true;
                isRunning = false;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !isCrouching)
            {
                jumpRequested = true;
            }
        }

        // Çarpışma kutusunu (Collider) ayarla: Alt tabanı zeminde sabit tutuyoruz
        if (isCrouching)
        {
            col.height = Mathf.Lerp(col.height, crouchHeight, Time.deltaTime * 15f);
            col.center = Vector3.Lerp(col.center, new Vector3(0f, -0.45f, 0f), Time.deltaTime * 15f);
        }
        else
        {
            col.height = Mathf.Lerp(col.height, originalColliderHeight, Time.deltaTime * 15f);
            col.center = Vector3.Lerp(col.center, originalColliderCenter, Time.deltaTime * 15f);
        }

        // Yön çevirme
        if (horizontalInput > 0)
            transform.rotation = Quaternion.Euler(0, 90f, 0);
        else if (horizontalInput < 0)
            transform.rotation = Quaternion.Euler(0, -90f, 0);

        UpdateAnimationsAndSteps();
    }

    // KRİTİK DÜZELTME: Eğilince ayakların yerin içine batmasını engellemek için modeli yukarı kaldırıyoruz
    private void LateUpdate()
    {
        if (visualModel != null)
        {
            float targetScaleY = isCrouching ? 0.6f : 1f;
            float targetPosY = isCrouching ? modelCrouchY : modelStandingY;

            // Boyutu ayarla
            visualModel.localScale = new Vector3(1f, targetScaleY, 1f);

            // Ayak pozisyonunu zemine göre hizala
            Vector3 currentPos = visualModel.localPosition;
            visualModel.localPosition = new Vector3(currentPos.x, Mathf.Lerp(currentPos.y, targetPosY, Time.deltaTime * 15f), currentPos.z);
        }
    }

    private void UpdateAnimationsAndSteps()
    {
        if (!hasAnimationSetup || !playableGraph.IsValid()) return;

        float targetIdle = 0f;
        float targetRun = 0f;
        float targetJump = 0f;

        if (!isGrounded && jumpClip != null)
        {
            targetJump = 1f;
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            targetRun = 1f;

            float stepSpeed = isRunning ? 1.5f : 1.0f;
            runTimer += Time.deltaTime * stepSpeed;
            if (runClip.length > 0 && runTimer >= runClip.length)
                runTimer -= runClip.length;

            runPlayable.SetTime(runTimer);
        }
        else
        {
            targetIdle = 1f;

            idleTimer += Time.deltaTime;
            if (idleClip.length > 0 && idleTimer >= idleClip.length)
                idleTimer -= idleClip.length;

            idlePlayable.SetTime(idleTimer);
        }

        float blendSpeed = Time.deltaTime * 12f;
        mixer.SetInputWeight(0, Mathf.MoveTowards(mixer.GetInputWeight(0), targetIdle, blendSpeed));
        mixer.SetInputWeight(1, Mathf.MoveTowards(mixer.GetInputWeight(1), targetRun, blendSpeed));

        if (jumpClip != null)
            mixer.SetInputWeight(2, Mathf.MoveTowards(mixer.GetInputWeight(2), targetJump, blendSpeed));
    }

    private void FixedUpdate()
    {
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
        else
            isGrounded = Physics.Raycast(transform.position, Vector3.down, (col.height / 2f) + 0.1f);

        float currentSpeed = walkSpeed;
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning)
            currentSpeed = runSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(horizontalInput * currentSpeed, currentVelocity.y, 0f);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
            jumpRequested = false;
        }

        if (transform.position.y < -10f)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = new Vector3(0f, 3f, 0f);
        }
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }
}