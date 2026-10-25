using UnityEngine;
using UnityEngine.InputSystem;

public class FirstLevelMaskUI : MonoBehaviour
{
    [SerializeField] private Transform mask;
    private PlayerController25D player;
    private bool reading;
    private readonly JournalBookView journal = new JournalBookView("Beast");
    private float previousTimeScale;
    private GUIStyle textStyle;
    private GUIStyle headingStyle;
    private bool NearMask => player != null && mask != null &&
        Vector3.Distance(player.transform.position, mask.position) < 2.3f;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController25D>();
        if (player != null) player.Respawned += Close;
    }

    private void Update()
    {
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
        reading = false;
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
        if (reading) { journal.Draw(Close); return; }
        if (!NearMask) return;
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            textStyle.normal.textColor = Color.white;
            headingStyle = new GUIStyle(textStyle) { fontSize = 26, fontStyle = FontStyle.Bold };
        }
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        {
            GUI.Box(new Rect(400, 610, 480, 65), GUIContent.none);
            GUI.Label(new Rect(420, 625, 440, 45), "[E / Tıkla] Çirkin'in maskesini incele", textStyle);
        }
        GUI.matrix = previous;
    }
}
