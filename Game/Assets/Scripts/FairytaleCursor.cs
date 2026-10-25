using UnityEngine;

// Hardware cursor stays responsive while gameplay is paused for books and conversations.
public sealed class FairytaleCursor : MonoBehaviour
{
    private static FairytaleCursor instance;
    private Texture2D cursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance) return;
        var host = new GameObject("Fairytale Mouse Cursor");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<FairytaleCursor>();
        instance.cursor = Resources.Load<Texture2D>("UI/FairytaleCursor");
        instance.Apply();
    }

    private void Apply()
    {
        if (!cursor) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(cursor, new Vector2(cursor.width * .055f, cursor.height * .075f), CursorMode.Auto);
    }

    private void OnApplicationFocus(bool focused) { if (focused) Apply(); }
    private void OnDestroy()
    {
        if (instance != this) return;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        instance = null;
    }
}
