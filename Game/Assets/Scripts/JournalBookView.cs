using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Draws the supplied spread as two hinged pages; its text remains part of the original artwork.
public sealed class JournalBookView
{
    private const float ArtworkAspect = 1303f / 845f;
    private readonly string resource;
    private Texture2D image;
    private float openedAt;
    private GUIStyle closeStyle, hintStyle;

    public JournalBookView(string resourceName) { resource = "Journals/" + resourceName; }

    public void Open()
    {
        if (!image) image = Resources.Load<Texture2D>(resource);
        openedAt = Time.unscaledTime;
        if (!image) Debug.LogError("Journal artwork missing: " + resource);
    }

    public static bool Clicked(Transform target)
    {
        if (!target || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return false;
        Camera camera = Camera.main;
        if (!camera) return false;
        Vector3 point = camera.WorldToScreenPoint(target.position);
        if (point.z <= 0) return false;
        float radius = camera.orthographic ? Screen.height / (camera.orthographicSize * 2) * .9f : 45;
        return Vector2.Distance(Mouse.current.position.ReadValue(), point) <= Mathf.Clamp(radius, 28, 80);
    }

    public void Draw(Action close)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color color = GUI.color, background = GUI.backgroundColor;
        int depth = GUI.depth;
        try
        {
            GUI.matrix = Matrix4x4.identity; GUI.depth = -1000;
            float t = Mathf.Clamp01((Time.unscaledTime - openedAt) / .7f);
            float ease = 1 - Mathf.Pow(1 - t, 3);
            GUI.color = new Color(0, 0, 0, .72f * ease);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            if (!image) { GUI.color = Color.white; if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 45), "Günlüğü kapat")) close(); return; }

            float height = Mathf.Min(Screen.height * .88f, Screen.width * .94f / ArtworkAspect);
            float width = height * ArtworkAspect;
            float x = (Screen.width - width) * .5f, y = (Screen.height - height) * .5f - 8 + (1 - ease) * 20;
            float spine = Screen.width * .5f, half = width * .5f;
            for (int i = 5; i >= 1; i--)
            {
                GUI.color = new Color(0, 0, 0, .045f * ease);
                GUI.DrawTexture(new Rect(x - i * 3, y + 8 - i * 2, width + i * 6, height + i * 4), Texture2D.whiteTexture);
            }
            float left = half * Mathf.Lerp(.04f, 1, ease);
            float rightT = Mathf.Clamp01((t - .09f) / .91f);
            float rightEase = 1 - Mathf.Pow(1 - rightT, 3);
            float right = half * Mathf.Lerp(.025f, 1, rightEase);
            GUI.color = new Color(Mathf.Lerp(.65f, 1, ease), Mathf.Lerp(.65f, 1, ease), Mathf.Lerp(.65f, 1, ease), ease);
            GUI.DrawTextureWithTexCoords(new Rect(spine - left, y, left, height), image, new Rect(0, 0, .5f, 1));
            GUI.color = new Color(Mathf.Lerp(.6f, 1, rightEase), Mathf.Lerp(.6f, 1, rightEase), Mathf.Lerp(.6f, 1, rightEase), ease);
            GUI.DrawTextureWithTexCoords(new Rect(spine, y, right, height), image, new Rect(.5f, 0, .5f, 1));
            if (t < 1)
            {
                GUI.color = new Color(1, .93f, .76f, Mathf.Sin(t * Mathf.PI) * .11f);
                GUI.DrawTexture(new Rect(spine + right * .8f, y + 8, Mathf.Max(2, right * .04f), height - 16), Texture2D.whiteTexture);
            }
            if (closeStyle == null)
            {
                closeStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
                hintStyle.normal.textColor = new Color(.92f, .86f, .72f);
            }
            GUI.color = new Color(1, 1, 1, ease);
            GUI.backgroundColor = new Color(.19f, .12f, .075f, 1);
            if (GUI.Button(new Rect(x + width - 40, Mathf.Max(6, y - 38), 36, 32), "×", closeStyle)) close();
            GUI.Label(new Rect(x, y + height + 8, width, 28), "E / Esc — Kapat", hintStyle);
        }
        finally { GUI.matrix = matrix; GUI.color = color; GUI.backgroundColor = background; GUI.depth = depth; }
    }
}
