using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ChaseFinishTrigger : MonoBehaviour
{
    private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController25D>() != null &&
            Scene2ChaseController.Instance != null)
            Scene2ChaseController.Instance.CompleteLevel();
    }
}
