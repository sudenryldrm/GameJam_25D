using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneExitTrigger : MonoBehaviour
{
    [Header("Sonraki Sahne Ayarı")]
    [SerializeField] private string nextSceneName = "Scene3_PrinceChase";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // Karakter platforma bastığında
        PlayerController25D player = other.GetComponentInParent<PlayerController25D>();
        if (player != null)
        {
            triggered = true;
            Debug.LogWarning($"<color=green>TEBRİKLER!</color> Kaçış platformuna ulaştın! Sonraki sahne: {nextSceneName}");
            
            // 3. Sahneyi açtığımızda alttaki satırın başındaki // işaretini kaldıracağız:
            // SceneManager.LoadScene(nextSceneName);
        }
    }
}