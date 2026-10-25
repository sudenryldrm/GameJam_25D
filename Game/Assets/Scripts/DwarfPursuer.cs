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
    private float targetGroundY;

    private void Awake() { EnsureReferences(); }
    private void OnEnable() { EnsureReferences(); }

    public void Bind(DwarfChaseGroup group, int index)
    {
        chase = group;
        order = index;
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (visual == null) visual = GetComponent<DwarfModelVisual>();
        if (chase == null) chase = GetComponentInParent<DwarfChaseGroup>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        GetComponent<CapsuleCollider>().isTrigger = true;
    }

    public void ResetToTrail()
    {
        EnsureReferences();
        if (rb == null || chase == null || !chase.Ready) return;
        progress = chase.DesiredDistance(order);
        Vector3 grounded = chase.GroundPoint(chase.Evaluate(progress));
        targetGroundY = grounded.y;
        rb.position = grounded + Vector3.up * 0.65f;
        if (visual != null) visual.SetMoving(false);
    }

    public float GroundY => targetGroundY;

    private void FixedUpdate()
    {
        EnsureReferences();
        if (chase == null || !chase.IsFollowing)
        {
            if (visual != null) visual.SetMoving(false);
            return;
        }
        progress = Mathf.MoveTowards(progress, chase.DesiredDistance(order), speed * Time.fixedDeltaTime);
        Vector3 grounded = chase.GroundPoint(chase.Evaluate(progress));
        targetGroundY = grounded.y;
        Vector3 next = grounded + Vector3.up * 0.65f;
        if (visual != null) visual.SetMoving((next - rb.position).sqrMagnitude > 0.000001f);
        if (Mathf.Abs(next.x - rb.position.x) > 0.001f)
            rb.MoveRotation(Quaternion.Euler(0f, next.x > rb.position.x ? 90f : -90f, 0f));
        rb.MovePosition(next);
    }
}
