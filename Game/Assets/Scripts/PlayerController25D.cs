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
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Animasyon Dosyaları")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip jumpClip;
    [SerializeField] private AnimationClip crouchClip;

    [Header("Görsel ve Eğilme Ayarları")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float modelStandingY = -1f;
    [SerializeField] private float modelCrouchY = -0.55f;

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

        int count = 3;
        if (crouchClip != null) count = 4;

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

            // Sol Ctrl veya C ile eğilme
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

        // Çarpışma kutusunun boyutu
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

    private void UpdateAnimationsAndSteps()
    {
        if (!hasAnimationSetup || !playableGraph.IsValid()) return;

        float targetIdle = 0f;
        float targetRun = 0f;
        float targetJump = 0f;
        float targetCrouch = 0f;

        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;

        // KRİTİK DÜZELTME: Eğilirken asla "havada zıplıyor" durumuna sokmuyoruz!
        if (!isGrounded && !isCrouching && jumpClip != null)
        {
            targetJump = 1f;
        }
        else if (isCrouching)
        {
            // EĞİLME DURUMU
            if (crouchClip != null)
            {
                targetCrouch = 1f;
                if (isMoving)
                {
                    crouchTimer += Time.deltaTime * 1.0f;
                    if (crouchClip.length > 0 && crouchTimer >= crouchClip.length) crouchTimer -= crouchClip.length;
                    crouchPlayable.SetTime(crouchTimer);
                }
            }
            else
            {
                // Özel animasyon yoksa mevcut koşan bacakları sinsi adımlarla zorla yürüt
                if (isMoving)
                {
                    targetRun = 1f;
                    runTimer += Time.deltaTime * 0.75f;
                    if (runClip.length > 0 && runTimer >= runClip.length) runTimer -= runClip.length;
                    runPlayable.SetTime(runTimer);
                }
                else
                {
                    targetIdle = 1f;
                }
            }
        }
        else if (isMoving)
        {
            // NORMAL AYAKTA KOŞMA
            targetRun = 1f;
            float stepSpeed = isRunning ? 1.5f : 1.0f;
            runTimer += Time.deltaTime * stepSpeed;
            if (runClip.length > 0 && runTimer >= runClip.length) runTimer -= runClip.length;
            runPlayable.SetTime(runTimer);
        }
        else
        {
            // NORMAL DURMA
            targetIdle = 1f;
            idleTimer += Time.deltaTime;
            if (idleClip.length > 0 && idleTimer >= idleClip.length) idleTimer -= idleClip.length;
            idlePlayable.SetTime(idleTimer);
        }

        // Yumuşak geçişler
        float blendSpeed = Time.deltaTime * 15f;
        mixer.SetInputWeight(0, Mathf.MoveTowards(mixer.GetInputWeight(0), targetIdle, blendSpeed));
        mixer.SetInputWeight(1, Mathf.MoveTowards(mixer.GetInputWeight(1), targetRun, blendSpeed));

        if (jumpClip != null)
            mixer.SetInputWeight(2, Mathf.MoveTowards(mixer.GetInputWeight(2), targetJump, blendSpeed));

        if (crouchClip != null)
            mixer.SetInputWeight(3, Mathf.MoveTowards(mixer.GetInputWeight(3), targetCrouch, blendSpeed));
    }

    private void LateUpdate()
    {
        if (visualModel != null)
        {
            float targetScaleY = (isCrouching && crouchClip == null) ? 0.6f : 1f;
            float targetPosY = (isCrouching && crouchClip == null) ? modelCrouchY : modelStandingY;

            visualModel.localScale = new Vector3(1f, targetScaleY, 1f);

            Vector3 currentPos = visualModel.localPosition;
            visualModel.localPosition = new Vector3(currentPos.x, Mathf.Lerp(currentPos.y, targetPosY, Time.deltaTime * 15f), currentPos.z);
        }
    }

    private void FixedUpdate()
    {
        // KRİTİK DÜZELTME: Zemin ışını collider'ın merkezinden yere doğru atılıyor, eğilince asla kopmuyor
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
        }
        else
        {
            Vector3 rayStart = transform.position + col.center;
            float rayLength = (col.height / 2f) + 0.3f;
            isGrounded = Physics.Raycast(rayStart, Vector3.down, rayLength);
        }

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