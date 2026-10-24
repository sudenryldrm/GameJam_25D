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
    [SerializeField] private float modelScale = 40f;
    [SerializeField] private bool fitModelToCollider = false;
    [SerializeField] private float modelStandingY = -1f;

    [HideInInspector] public bool isInteracting = false;

    public event System.Action Respawned;

    private Vector3 spawnPosition;
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
        spawnPosition = transform.position;
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
        if (fitModelToCollider) FitVisualModel();
        jumpsRemaining = maxJumps;
    }

    // Fit the evaluated skin, since retargeting can change its bounds from the source mesh.
    private void FitVisualModel()
    {
        if (visualModel == null)
        {
            Debug.LogError("Player visual model is missing.", this);
            return;
        }

        visualModel.localScale = Vector3.one;
        if (anim != null) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (playableGraph.IsValid()) playableGraph.Evaluate(0f);
        if (!TryGetVisualBounds(out Bounds bounds) || bounds.size.y <= 0f)
        {
            Debug.LogError("Player model has no measurable mesh. Check its FBX import.", this);
            return;
        }

        modelScale = col.height / bounds.size.y;
        // Scale the measured bounds around the visual root, not around world zero.
        Vector3 origin = visualModel.localPosition;
        Vector3 scaledCenter = origin + (bounds.center - origin) * modelScale;
        float scaledBottom = origin.y + (bounds.min.y - origin.y) * modelScale;
        Vector3 position = origin;
        position.x += col.center.x - scaledCenter.x;
        position.z += col.center.z - scaledCenter.z;
        position.y += col.center.y - col.height * 0.5f - scaledBottom;
        visualModel.localScale = Vector3.one * modelScale;
        visualModel.localPosition = position;
        modelStandingY = position.y;
    }

    private bool TryGetVisualBounds(out Bounds result)
    {
        result = default;
        bool found = false;
        foreach (Renderer renderer in visualModel.GetComponentsInChildren<Renderer>(true))
        {
            Bounds localBounds;
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                // Measure actual deformed vertices once, without trusting import bounds.
                skinned.updateWhenOffscreen = true;
                Mesh snapshot = new Mesh();
                try
                {
                    skinned.BakeMesh(snapshot, false);
                    if (snapshot.vertexCount == 0) continue;
                    snapshot.RecalculateBounds();
                    localBounds = snapshot.bounds;
                }
                finally
                {
                    Destroy(snapshot);
                }
            }
            else if (renderer is MeshRenderer &&
                     renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                localBounds = filter.sharedMesh.bounds;
            else
                continue;

            Matrix4x4 toPlayer = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = localBounds.center + Vector3.Scale(localBounds.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f,
                                (i & 2) == 0 ? -1f : 1f,
                                (i & 4) == 0 ? -1f : 1f));
                Vector3 point = toPlayer.MultiplyPoint3x4(corner);
                if (!found) { result = new Bounds(point, Vector3.zero); found = true; }
                else result.Encapsulate(point);
            }
        }
        return found;
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
        isGrounded = Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, rayLength, ~0, QueryTriggerInteraction.Ignore)
            && groundHit.normal.y > 0.35f;

        // Hız seçimi
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        float targetVelocityX = horizontalInput * targetSpeed;

        // Tok İvmelenme
        float accelRate = (Mathf.Abs(targetVelocityX) > 0.01f) ? acceleration : deceleration;
        currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocityX, accelRate * Time.fixedDeltaTime);

        Vector3 vel = rb.linearVelocity;
        vel.x = currentVelocityX;

        // Follow marked stair slopes while grounded; a jump still leaves the slope.
        bool onStairs = isGrounded && groundHit.collider.GetComponent<StairRamp>() != null && vel.y < jumpForce * 0.5f;
        if (onStairs)
            vel.y = -groundHit.normal.x / groundHit.normal.y * vel.x;
        // Değişken Zıplama Yüksekliği
        else if (vel.y < 0)
            vel.y += Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (vel.y > 0 && !isJumpPressed)
            vel.y += Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;

        rb.linearVelocity = vel;

        if (transform.position.y < -10f)
        {
            Respawn();
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        spawnPosition = position;
    }

    public void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spawnPosition;
        horizontalInput = 0f;
        currentVelocityX = 0f;
        isGrounded = false;
        isRunning = false;
        isJumpPressed = false;
        isInteracting = false;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        jumpsRemaining = maxJumps;
        Respawned?.Invoke();
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
            // Preserve the configured or measured visual scale.
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