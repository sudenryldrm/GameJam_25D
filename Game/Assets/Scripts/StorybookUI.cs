using UnityEngine;

public static class StorybookUI
{
    private static Texture2D parchment;
    private static GUIStyle panel, title, body, small, centered, button, hint;
    private static Font regularFont, boldFont;
    private static readonly Color Ink = new Color(.19f, .12f, .075f);

    public static Font RegularFont => regularFont ? regularFont : regularFont = Resources.Load<Font>("UI/Storybook-Regular");
    public static Font BoldFont => boldFont ? boldFont : boldFont = Resources.Load<Font>("UI/Storybook-Bold");

    private static void Prepare()
    {
        if (panel != null && parchment) return;
        const int n = 64;
        parchment = new Texture2D(n, n, TextureFormat.RGBA32, false)
        { name = "Storybook Parchment", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(12 - x, x - 51), dy = Mathf.Max(12 - y, y - 51);
                float distance = Mathf.Sqrt(Mathf.Pow(Mathf.Max(dx, 0), 2) + Mathf.Pow(Mathf.Max(dy, 0), 2)) + Mathf.Min(Mathf.Max(dx, dy), 0) - 12;
                float grain = Mathf.Sin(x * 37.1f + y * 71.7f) * .013f;
                Color c = new Color(.83f + grain, .75f + grain, .59f + grain, .98f);
                if (distance > -2.5f) c = new Color(.22f, .13f, .075f);
                else if (distance > -4.5f) c = new Color(.65f, .45f, .23f);
                else if (distance > -6) c = new Color(.4f, .26f, .13f);
                c.a *= Mathf.Clamp01(.5f - distance);
                pixels[y * n + x] = c;
            }
        parchment.SetPixels(pixels); parchment.Apply(false, true);
        panel = new GUIStyle { border = new RectOffset(16, 16, 16, 16) };
        panel.normal.background = parchment;
        title = Label(25, true, TextAnchor.MiddleLeft);
        body = Label(19, false, TextAnchor.MiddleLeft);
        small = Label(16, false, TextAnchor.MiddleLeft);
        centered = Label(21, true, TextAnchor.MiddleCenter);
        hint = Label(16, false, TextAnchor.MiddleCenter);
        button = new GUIStyle(panel) { font = BoldFont, fontSize = 24, alignment = TextAnchor.MiddleCenter };
        button.normal.textColor = Ink;
        button.hover.background = parchment; button.hover.textColor = new Color(.5f, .21f, .08f);
        button.active.background = parchment; button.active.textColor = Ink;
    }

    private static GUIStyle Label(int size, bool bold, TextAnchor alignment)
    {
        var style = new GUIStyle(GUI.skin.label)
        { font = bold ? BoldFont : RegularFont, fontSize = size, alignment = alignment, wordWrap = true };
        style.normal.textColor = Ink;
        return style;
    }

    public static Matrix4x4 Begin()
    {
        Prepare();
        Matrix4x4 previous = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) * .5f, (Screen.height - 720 * scale) * .5f, 0), Quaternion.identity, Vector3.one * scale);
        return previous;
    }

    public static void Panel(Rect rect)
    {
        Prepare(); Color previous = GUI.color;
        GUI.color = new Color(0, 0, 0, .27f);
        GUI.Box(new Rect(rect.x + 3, rect.y + 5, rect.width, rect.height), GUIContent.none, panel);
        GUI.color = Color.white; GUI.Box(rect, GUIContent.none, panel); GUI.color = previous;
    }

    public static void Chapter(string numeral, string name)
    {
        Panel(new Rect(24, 22, 350, 78));
        GUI.Label(new Rect(43, 31, 310, 20), "MASALIN İZİNDE  /  BÖLÜM " + numeral, small);
        GUI.Label(new Rect(43, 52, 310, 36), name, title);
    }

    public static void Stats(string text)
    {
        Panel(new Rect(1056, 22, 200, 74));
        GUI.Label(new Rect(1072, 32, 168, 54), text, small);
    }

    public static void Controls()
    {
        Panel(new Rect(24, 662, 590, 36));
        GUI.Label(new Rect(40, 667, 558, 25), "A / D  Yürü     Shift  Koş     Space  Zıpla     R  Yeniden başla", small);
    }

    public static void Prompt(string text)
    {
        Panel(new Rect(360, 557, 560, 80));
        GUI.Label(new Rect(382, 566, 516, 32), text, centered);
        GUI.Label(new Rect(382, 600, 516, 24), "E  veya  tıkla  ·  Günlüğü aç", hint);
    }

    public static void Notice(string text)
    {
        Panel(new Rect(350, 116, 580, 64));
        GUI.Label(new Rect(370, 125, 540, 46), text, body);
    }

    public static void Dialog(string heading, string text)
    {
        Panel(new Rect(310, 248, 660, 200));
        GUI.Label(new Rect(340, 270, 600, 48), heading, title);
        GUI.Label(new Rect(340, 326, 600, 92), text, body);
    }

    public static void SkipPrompt()
    {
        Matrix4x4 previous = Begin();
        Panel(new Rect(420, 650, 440, 44));
        GUI.Label(new Rect(440, 659, 400, 28), "E / Space / Esc  ·  Hikâyeye devam et", small);
        GUI.matrix = previous;
    }

    public static bool CloseButton(Rect rect)
    { Prepare(); return GUI.Button(rect, "×", button); }

    public static bool Conversation(string speaker, string text, int line, int total)
    {
        Prepare();
        Color color = GUI.color;
        GUI.color = new Color(0, 0, 0, .8f);
        GUI.DrawTexture(new Rect(-1280, -720, 3840, 764), Texture2D.whiteTexture);
        GUI.color = color;
        Panel(new Rect(112, 516, 1056, 188));
        Panel(new Rect(140, 494, 290, 48));
        GUI.Label(new Rect(159, 501, 254, 34), speaker, title);
        GUI.Label(new Rect(148, 549, 984, 92), text, body);
        GUI.Label(new Rect(148, 661, 740, 25), "E / Tıkla  ·  Devam et                 Esc  ·  Sahneyi geç", small);
        GUI.Label(new Rect(1030, 661, 100, 25), line + " / " + total, small);
        return GUI.Button(new Rect(920, 651, 94, 40), "Devam", button);
    }
}
