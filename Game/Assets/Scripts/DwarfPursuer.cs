using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class DwarfPursuer : MonoBehaviour
{
    [SerializeField] private DwarfChaseGroup chase;
    [SerializeField] private int order;
    [SerializeField] private float speed = 10.5f;
    private Rigidbody rb;
    private float progress;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        GetComponent<CapsuleCollider>().isTrigger = true;
    }

    public void ResetToTrail()
    {
        if (rb == null || chase == null || !chase.Ready) return;
        progress = chase.DesiredDistance(order);
        rb.position = chase.Evaluate(progress) + Vector3.up * 0.65f;
    }

    private void FixedUpdate()
    {
        if (chase == null || !chase.IsFollowing) return;
        progress = Mathf.MoveTowards(progress, chase.DesiredDistance(order), speed * Time.fixedDeltaTime);
        Vector3 next = chase.Evaluate(progress) + Vector3.up * 0.65f;
        if (Mathf.Abs(next.x - rb.position.x) > 0.001f)
            rb.MoveRotation(Quaternion.Euler(0f, next.x > rb.position.x ? 90f : -90f, 0f));
        rb.MovePosition(next);
    }
}
