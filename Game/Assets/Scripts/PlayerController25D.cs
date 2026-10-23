using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Hareket Hızları ve İvme")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8.5f;
    [SerializeField] private float crouchSpeed = 2.2f;
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

    [Header("Animasyon Dosyaları")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip jumpClip;
    [SerializeField] private AnimationClip crouchClip;

    [Header("Görsel Boyut ve Yükseklik Ayarları")]
    [SerializeField] private Transform visualModel;
    [Tooltip("Karakter çok küçükse bu sayıyı 10, 50 veya 100 yapabilirsin")]
    [SerializeField] private float modelScale = 1f;         // Karıncalığı düzelten büyütme çarpanı
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float modelStandingY = -1f;
    [SerializeField] private float modelCrouchY = -0.55f;

    [HideInInspector] public bool isInteracting = false;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Animator anim;
    private float horizontalInput;
    private bool isGrounded;
    private bool isCrouching;
    private bool isRunning;
    private bool isJumpPressed;

    private int jumpsRemaining;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float currentVelocityX;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable runPlayable;
    private AnimationClipPlayable jumpPlayable;
    private AnimationClipPlayable crouchPlayable;
    private bool hasAnimationSetup = false;

    private double idleTimer = 0.0;
    private double runTimer = 0.0;
    private double crouchTimer = 0.0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        originalColliderHeight = col.height;
        originalColliderCenter = col.center;

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

        int count = (crouchClip != null) ? 4 : 3;
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

        if (crouchClip != null)
        {
            crouchPlayable = AnimationClipPlayable.Create(playableGraph, crouchClip);
            crouchClip.wrapMode = WrapMode.Loop;
            playableGraph.Connect(crouchPlayable, 0, mixer, 3);
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

            isJumpPressed = Keyboard.current.spaceKey.isPressed;

            if (Keyboard.current.spaceKey.wasPressedThisFrame && !isCrouching)
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
        Vector3 rayStart = transform.position + col.center;
        float rayLength = (col.height / 2f) + 0.25f;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, rayLength);

        float targetSpeed = walkSpeed;
        if (isCrouching)
            targetSpeed = crouchSpeed;
        else if (isRunning)
            targetSpeed = runSpeed;

        float targetVelocityX = horizontalInput * targetSpeed;

        float accelRate = (Mathf.Abs(targetVelocityX) > 0.01f) ? acceleration : deceleration;
        currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocityX, accelRate * Time.fixedDeltaTime);

        Vector3 vel = rb.linearVelocity;
        vel.x = currentVelocityX;

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
        float targetCrouch = 0f;

        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;

        if (!isGrounded && !isCrouching && jumpClip != null)
        {
            targetJump = 1f;
        }
        else if (isCrouching)
        {
            if (crouchClip != null)
            {
                targetCrouch = 1f;
                if (isMoving)
                {
                    crouchTimer += Time.deltaTime;
                    if (crouchClip.length > 0 && crouchTimer >= crouchClip.length) crouchTimer -= crouchClip.length;
                    crouchPlayable.SetTime(crouchTimer);
                }
            }
            else
            {
                if (isMoving)
                {
                    targetRun = 1f;
                    runTimer += Time.deltaTime * 0.7f;
                    if (runClip.length > 0 && runTimer >= runClip.length) runTimer -= runClip.length;
                    runPlayable.SetTime(runTimer);
                }
                else targetIdle = 1f;
            }
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

        if (crouchClip != null)
            mixer.SetInputWeight(3, Mathf.MoveTowards(mixer.GetInputWeight(3), targetCrouch, blendSpeed));
    }

    // BOYUTU BURADAN BÜYÜTÜYORUZ
    private void LateUpdate()
    {
        if (visualModel != null)
        {
            float targetScaleY = (isCrouching && crouchClip == null) ? (0.6f * modelScale) : modelScale;
            float targetPosY = (isCrouching && crouchClip == null) ? modelCrouchY : modelStandingY;

            visualModel.localScale = new Vector3(modelScale, targetScaleY, modelScale);

            Vector3 currentPos = visualModel.localPosition;
            visualModel.localPosition = new Vector3(currentPos.x, Mathf.Lerp(currentPos.y, targetPosY, Time.deltaTime * 15f), currentPos.z);
        }
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }
}