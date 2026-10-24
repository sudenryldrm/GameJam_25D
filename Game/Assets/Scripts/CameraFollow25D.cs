using UnityEngine;

public class CameraFollow25D : MonoBehaviour
{
    [Header("Hedef")]
    [SerializeField] private Transform target;

    [Header("Takip Ayarları")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    [SerializeField] private bool lockHorizontalPosition = false;
    [SerializeField] private float horizontalPosition = 0f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Kameranın gitmek istediği pozisyon (Z eksenini sabit tutuyoruz)
        Vector3 targetPosition = new Vector3(lockHorizontalPosition ? horizontalPosition : target.position.x + offset.x, target.position.y + offset.y, offset.z);

        // Yumuşak takip hareketi (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
