using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Hareket Hızları ve İvme (GDD Standartları)")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8.5f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float deceleration = 30f;

    [Header("Zıplama ve Çift Zıplama")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float doubleJumpForce = 11f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Animasyon Dosyaları (3 Temel Animasyon)")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip jumpClip;

    [Header("Görsel Ölçek Ayarı")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private float modelScale = 40f;       // Sisteme kaydettiğimiz 40 ölçeği
    [SerializeField] private float modelStandingY = -1f;

    [HideInInspector] public bool isInteracting = false;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Animator anim;
    private float horizontalInput;
    private bool isGrounded;
    private bool isRunning;
    private bool isJumpPressed;

    private int jumpsRemaining;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float currentVelocityX;

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

        if (visualModel == null && transform.childCount > 0)
            visualModel = transform.GetChild(0);

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
        jumpsRemaining = maxJumps;
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
        if (isInteracting)
        {
            horizontalInput = 0f;
            return;
        }

        horizontalInput = 0f;
        isRunning = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                horizontalInput = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                horizontalInput = 1f;

            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                isRunning = true;

            isJumpPressed = Keyboard.current.spaceKey.isPressed;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (coyoteCounter > 0f)
                {
                    ExecuteJump(jumpForce);
                    jumpsRemaining = maxJumps - 1;
                    coyoteCounter = 0f;
                }
                else if (jumpsRemaining > 0)
                {
                    ExecuteJump(doubleJumpForce);
                    jumpsRemaining--;
                }
                else
                {
                    jumpBufferCounter = jumpBufferTime;
                }
            }
        }

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            jumpsRemaining = maxJumps;

            if (jumpBufferCounter > 0f)
            {
                ExecuteJump(jumpForce);
                jumpsRemaining = maxJumps - 1;
                jumpBufferCounter = 0f;
            }
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        jumpBufferCounter -= Time.deltaTime;

        // Yön çevirme
        if (horizontalInput > 0)
            transform.rotation = Quaternion.Euler(0, 90f, 0);
        else if (horizontalInput < 0)
            transform.rotation = Quaternion.Euler(0, -90f, 0);

        UpdateAnimations();
    }

    private void ExecuteJump(float force)
    {
        Vector3 vel = rb.linearVelocity;
        vel.y = force;
        rb.linearVelocity = vel;
    }

    private void FixedUpdate()
    {
        // Zemin kontrolü
        Vector3 rayStart = transform.position + col.center;
        float rayLength = (col.height / 2f) + 0.25f;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, rayLength);

        // Hız seçimi
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        float targetVelocityX = horizontalInput * targetSpeed;

        // Tok İvmelenme
        float accelRate = (Mathf.Abs(targetVelocityX) > 0.01f) ? acceleration : deceleration;
        currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocityX, accelRate * Time.fixedDeltaTime);

        Vector3 vel = rb.linearVelocity;
        vel.x = currentVelocityX;

        // Değişken Zıplama Yüksekliği
        if (vel.y < 0)
            vel.y += Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (vel.y > 0 && !isJumpPressed)
            vel.y += Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;

        rb.linearVelocity = vel;

        if (transform.position.y < -10f)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = new Vector3(0f, 3f, 0f);
        }
    }

    private void UpdateAnimations()
    {
        if (!hasAnimationSetup || !playableGraph.IsValid()) return;

        float targetIdle = 0f;
        float targetRun = 0f;
        float targetJump = 0f;

        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;

        if (!isGrounded && jumpClip != null)
        {
            targetJump = 1f;
        }
        else if (isMoving)
        {
            targetRun = 1f;
            float stepSpeed = isRunning ? 1.5f : 1.0f;
            runTimer += Time.deltaTime * stepSpeed;
            if (runClip.length > 0 && runTimer >= runClip.length) runTimer -= runClip.length;
            runPlayable.SetTime(runTimer);
        }
        else
        {
            targetIdle = 1f;
            idleTimer += Time.deltaTime;
            if (idleClip.length > 0 && idleTimer >= idleClip.length) idleTimer -= idleClip.length;
            idlePlayable.SetTime(idleTimer);
        }

        float blendSpeed = Time.deltaTime * 15f;
        mixer.SetInputWeight(0, Mathf.MoveTowards(mixer.GetInputWeight(0), targetIdle, blendSpeed));
        mixer.SetInputWeight(1, Mathf.MoveTowards(mixer.GetInputWeight(1), targetRun, blendSpeed));

        if (jumpClip != null)
            mixer.SetInputWeight(2, Mathf.MoveTowards(mixer.GetInputWeight(2), targetJump, blendSpeed));
    }

    private void LateUpdate()
    {
        if (visualModel != null)
        {
            // Model her zaman orijinal 40 ölçeğinde ve dik kalır
            visualModel.localScale = new Vector3(modelScale, modelScale, modelScale);

            Vector3 currentPos = visualModel.localPosition;
            visualModel.localPosition = new Vector3(currentPos.x, Mathf.Lerp(currentPos.y, modelStandingY, Time.deltaTime * 15f), currentPos.z);
        }
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }
}