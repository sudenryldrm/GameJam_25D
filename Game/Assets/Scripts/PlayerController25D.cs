using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Hareket Hızları")]
    [SerializeField] private float walkSpeed = 4f;        // Normal yürüme
    [SerializeField] private float runSpeed = 10f;        // Shift ile hızlı koşma
    [SerializeField] private float crouchSpeed = 2f;      // Eğilerek yavaş yürüme
    [SerializeField] private float jumpForce = 12f;

    [Header("Görsel Boyut Ayarları")]
    [SerializeField] private Transform visualModel;       // İçine koyduğumuz 3D karakter
    [SerializeField] private float crouchHeight = 1f;

    [Header("Zemin Kontrolü")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private CapsuleCollider col;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpRequested;
    private bool isCrouching;
    private bool isRunning;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private Vector3 originalVisualScale = Vector3.one;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        originalColliderHeight = col.height;
        originalColliderCenter = col.center;

        // Eğer visualModel atanmadıysa ilk alt nesneyi (çocuğu) otomatik bul
        if (visualModel == null && transform.childCount > 0)
        {
            visualModel = transform.GetChild(0);
        }

        if (visualModel != null)
        {
            originalVisualScale = visualModel.localScale;
        }
    }

    private void Update()
    {
        horizontalInput = 0f;
        isRunning = false;
        isCrouching = false;

        if (Keyboard.current != null)
        {
            // Sağ - Sol Hareketi (A / D veya Oklar)
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                horizontalInput = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                horizontalInput = 1f;

            // Shift ile Koşma
            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
            {
                isRunning = true;
            }

            // Ctrl veya C tuşu ile Eğilme (C tuşu çok pratiktir)
            if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed)
            {
                isCrouching = true;
                isRunning = false; // Eğilirken koşulamaz
            }

            // Zıplama (Boşluk / Space)
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !isCrouching)
            {
                jumpRequested = true;
            }
        }

        // Karakterin görsel ve fiziksel boyunu eğilmeye göre küçült/büyüt
        HandleCrouchVisuals();

        // Karakterin baktığı yönü çevirme
        if (horizontalInput > 0)
        {
            transform.rotation = Quaternion.Euler(0, 90f, 0);
        }
        else if (horizontalInput < 0)
        {
            transform.rotation = Quaternion.Euler(0, -90f, 0);
        }
    }

    private void FixedUpdate()
    {
        // Zemin kontrolü
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
        else
            isGrounded = Physics.Raycast(transform.position, Vector3.down, (col.height / 2f) + 0.1f);

        // Hız seçimi
        float currentSpeed = walkSpeed;
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning)
            currentSpeed = runSpeed;

        // Hareketi uygula
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(horizontalInput * currentSpeed, currentVelocity.y, 0f);

        // Zıplamayı uygula
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
            jumpRequested = false;
        }

        // Boşluğa düşerse geri ışınla
        if (transform.position.y < -10f)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = new Vector3(0f, 3f, 0f);
        }
    }

    private void HandleCrouchVisuals()
    {
        if (isCrouching)
        {
            // Fizik kutusunu küçült
            col.height = Mathf.Lerp(col.height, crouchHeight, Time.deltaTime * 15f);
            col.center = Vector3.Lerp(col.center, new Vector3(0f, -0.5f, 0f), Time.deltaTime * 15f);

            // 3D Karakter modelini görsel olarak da ez/çömel (Y boyunu yarıya indir)
            if (visualModel != null)
            {
                Vector3 crouchScale = new Vector3(originalVisualScale.x, originalVisualScale.y * 0.5f, originalVisualScale.z);
                visualModel.localScale = Vector3.Lerp(visualModel.localScale, crouchScale, Time.deltaTime * 15f);
            }
        }
        else
        {
            // Eski normal haline dön
            col.height = Mathf.Lerp(col.height, originalColliderHeight, Time.deltaTime * 15f);
            col.center = Vector3.Lerp(col.center, originalColliderCenter, Time.deltaTime * 15f);

            if (visualModel != null)
            {
                visualModel.localScale = Vector3.Lerp(visualModel.localScale, originalVisualScale, Time.deltaTime * 15f);
            }
        }
    }
}