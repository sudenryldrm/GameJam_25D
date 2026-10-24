using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TowerCheckpoint : MonoBehaviour
{
    [SerializeField] private int routeIndex;
    private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController25D>() != null && TowerLevelController.Instance != null)
            TowerLevelController.Instance.SetCheckpoint(transform.position + Vector3.up * 0.2f, routeIndex);
    }
}
