using UnityEngine;

// Uses the evaluated skeleton, not the FBX's assumed forward axis.
public sealed class ConversationActorPose
{
    private readonly Transform root, leftArm, rightArm;
    private readonly Rigidbody body;
    private readonly RigidbodyInterpolation interpolation;
    private Quaternion heldRotation;

    public ConversationActorPose(Transform actor)
    {
        root = actor;
        body = actor.GetComponent<Rigidbody>();
        if (body) { interpolation = body.interpolation; body.interpolation = RigidbodyInterpolation.None; }
        Animator animator = actor.GetComponentInChildren<Animator>();
        if (animator && animator.avatar && animator.avatar.isValid && animator.isHuman)
        {
            leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        }
        if (!leftArm || !rightArm)
            foreach (Transform bone in actor.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name.ToLowerInvariant();
                if (name.EndsWith("leftarm") || name.EndsWith("leftupperarm")) leftArm = bone;
                if (name.EndsWith("rightarm") || name.EndsWith("rightupperarm")) rightArm = bone;
            }
        heldRotation = actor.rotation;
    }

    public void Face(Vector3 destination)
    {
        if (!root) return;
        Vector3 desired = Vector3.ProjectOnPlane(destination - root.position, Vector3.up);
        Vector3 forward = leftArm && rightArm
            ? Vector3.Cross(rightArm.position - leftArm.position, Vector3.up) : root.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (desired.sqrMagnitude > .01f && forward.sqrMagnitude > .001f)
            heldRotation = Quaternion.AngleAxis(Vector3.SignedAngle(forward, desired, Vector3.up), Vector3.up) * root.rotation;
        Set(root.position, heldRotation);
    }

    public void Ground(float hover = 0)
    {
        if (!root) return;
        Collider capsule = root.GetComponent<CapsuleCollider>();
        if (!capsule) return;
        RaycastHit[] hits = Physics.RaycastAll(root.position + Vector3.up * 2, Vector3.down, 12, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity, ground = 0;
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(root) || hit.collider.GetComponentInParent<PlayerController25D>() ||
                hit.collider.GetComponentInParent<DwarfPursuer>() || hit.collider.GetComponentInParent<PrinceChaserAI>() ||
                hit.collider.GetComponentInParent<WitchBossAI>()) continue;
            if (hit.distance >= best) continue;
            best = hit.distance; ground = hit.point.y; found = true;
        }
        if (found) Set(root.position + Vector3.up * (ground + .015f + hover - capsule.bounds.min.y), root.rotation);
    }

    public void Set(Vector3 position, Quaternion rotation)
    {
        if (!root) return;
        if (body) { body.position = position; body.rotation = rotation; }
        root.SetPositionAndRotation(position, rotation);
    }

    public void Release()
    {
        if (body) body.interpolation = interpolation;
    }
}
