using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Zemin Kontrolü")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Klavyeden A-D veya Sol-Sağ ok tuşlarını okuma
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Karakterin baktığı yönü çevirme (Sağa/Sola dönme)
        if (horizontalInput > 0)
        {
            transform.rotation = Quaternion.Euler(0, 90f, 0);
        }
        else if (horizontalInput < 0)
        {
            transform.rotation = Quaternion.Euler(0, -90f, 0);
        }

        // Zıplama tuşu (Boşluk / Space)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        // Zemin kontrolü
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
        }
        else
        {
            // Eğer groundCheck atanmadıysa basit ışın (raycast) kontrolü
            isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }

        // Yatay hareket (Unity 6 için linearVelocity)
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(horizontalInput * moveSpeed, currentVelocity.y, 0f);

        // Zıplama uygulama
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
            jumpRequested = false;
        }
    }

    // Editörde zemin kontrol alanını görebilmek için
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}