using UnityEngine;
using UnityEngine.InputSystem;

public class FirstLevelMaskUI : MonoBehaviour
{
    [SerializeField] private Transform mask;
    private PlayerController25D player;
    private bool reading;
    private readonly JournalBookView journal = new JournalBookView("Beast");
    private float previousTimeScale;
    private bool NearMask => player != null && mask != null &&
        Vector3.Distance(player.transform.position, mask.position) < 2.3f;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController25D>();
        if (player != null) player.Respawned += Close;
    }

    private void Update()
    {
        if (LevelIntroCutscene.IsPlaying || StorybookCoverScreen.IsShowing) return;
        if (player == null) return;
        if (NearMask && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (!reading && JournalBookView.Clicked(mask))))
        {
            if (reading) Close();
            else
            {
                reading = true;
                journal.Open();
                previousTimeScale = Time.timeScale;
                player.isInteracting = true;
                Time.timeScale = 0f;
            }
        }
        if (reading && (!NearMask || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))) Close();
    }

    private void Close()
    {
        if (!reading) return;
        reading = false; journal.Close();
        Time.timeScale = previousTimeScale;
        if (player != null) player.isInteracting = false;
    }

    private void OnDisable()
    {
        Close();
        if (player != null) player.Respawned -= Close;
    }

    private void OnGUI()
    {
        if (LevelIntroCutscene.IsPlaying || StorybookCoverScreen.IsShowing) return;
        if (reading) { journal.Draw(Close); return; }
        if (player == null || LevelIntroCutscene.IsPlaying) return;
        Matrix4x4 previous = StorybookUI.Begin();
        StorybookUI.Chapter("I", "Ormanın Fısıltısı");
        StorybookUI.Controls();
        if (NearMask) StorybookUI.Prompt("Çirkin'in Maskesi");
        GUI.matrix = previous;
    }
}
