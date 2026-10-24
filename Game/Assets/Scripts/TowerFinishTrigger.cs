using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TowerFinishTrigger : MonoBehaviour
{
    private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController25D>() != null && TowerLevelController.Instance != null)
            TowerLevelController.Instance.CompleteLevel();
    }
}
