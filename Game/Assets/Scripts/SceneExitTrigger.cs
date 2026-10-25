using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public class SceneExitTrigger : MonoBehaviour
{
    [Header("Sonraki Sahne")]
    [SerializeField] private string nextSceneName = "Scene2_WitchChase";

    private bool triggered;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || other.GetComponentInParent<PlayerController25D>() == null)
            return;

        if (string.IsNullOrWhiteSpace(nextSceneName) ||
            !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"Scene exit cannot load '{nextSceneName}'. Add the scene to the build scene list.", this);
            return;
        }

        // Latch before loading so multiple player colliders cannot load twice.
        triggered = true;
        StorySfx.Play("Complete", .35f);
        other.GetComponentInParent<PlayerController25D>().isInteracting = true;
        SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider area = GetComponent<BoxCollider>();
        if (area == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.25f);
        Gizmos.DrawCube(area.center, area.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
