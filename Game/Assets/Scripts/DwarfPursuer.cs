using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class DwarfPursuer : MonoBehaviour
{
    [SerializeField] private DwarfChaseGroup chase;
    [SerializeField] private int order;
    [SerializeField] private float speed = 10.5f;
    private Rigidbody rb;
    private float progress;
    private DwarfModelVisual visual;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        visual = GetComponent<DwarfModelVisual>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        GetComponent<CapsuleCollider>().isTrigger = true;
    }

    public void ResetToTrail()
    {
        if (rb == null || chase == null || !chase.Ready) return;
        progress = chase.DesiredDistance(order);
        rb.position = chase.GroundPoint(chase.Evaluate(progress)) + Vector3.up * 0.65f;
        if (visual != null) visual.SetMoving(false);
    }

    private void FixedUpdate()
    {
        if (chase == null || !chase.IsFollowing)
        {
            if (visual != null) visual.SetMoving(false);
            return;
        }
        progress = Mathf.MoveTowards(progress, chase.DesiredDistance(order), speed * Time.fixedDeltaTime);
        Vector3 next = chase.GroundPoint(chase.Evaluate(progress)) + Vector3.up * 0.65f;
        if (visual != null) visual.SetMoving((next - rb.position).sqrMagnitude > 0.000001f);
        if (Mathf.Abs(next.x - rb.position.x) > 0.001f)
            rb.MoveRotation(Quaternion.Euler(0f, next.x > rb.position.x ? 90f : -90f, 0f));
        rb.MovePosition(next);
    }
}
