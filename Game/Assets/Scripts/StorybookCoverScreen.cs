using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// The supplied artwork remains intact; the coloured portrait is the only start control.
public sealed class StorybookCoverScreen : MonoBehaviour
{
    private static bool gameStarted;
    public static bool IsShowing { get; private set; }
    public static bool NeedsStart => !gameStarted;
    private Texture2D artwork, glow;
    private Action onStart;
    private PlayerController25D player;
    private const float OpenDuration = 1.65f, CloseDuration = 1.4f, EndingHold = 8f;
    private float previousTimeScale, screenAge, closeAt = -1;
    private bool previousInteraction, visible, ending, loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { gameStarted = false; IsShowing = false; }

    public void ShowStart(Action begin) { Show(false, begin); }
    public void ShowEnding() { Show(true, null); }

    private void Show(bool final, Action begin)
    {
        if (visible) return;
        artwork = Resources.Load<Texture2D>(final ? "UI/Ending" : "UI/StartCover");
        if (!artwork) { Debug.LogError("Storybook cover artwork is missing."); begin?.Invoke(); return; }
        FirstLevelMusic.Stop();
        FirstLevelMusic.Cover(true);
        ending = final; onStart = begin; screenAge = 0; closeAt = -1; loading = false;
        player = FindFirstObjectByType<PlayerController25D>();
        previousTimeScale = Time.timeScale;
        previousInteraction = player && player.isInteracting;
        if (player) player.isInteracting = true;
        Time.timeScale = 0;
        visible = true; IsShowing = true;
        StorySfx.Play("BookOpen", .7f);
        if (ending) StorySfx.Play("Complete", .34f);
        if (!glow) CreateGlow();
    }

    private Rect BookRect()
    {
        float aspect = (float)artwork.width / artwork.height;
        float height = Mathf.Min(Screen.height * .94f, Screen.width * .96f / aspect);
        return new Rect((Screen.width - height * aspect) * .5f, (Screen.height - height) * .5f, height * aspect, height);
    }

    private bool OverPortrait(Vector2 point, Rect book)
    {
        Vector2 normalized = new Vector2((point.x - book.x) / book.width, (point.y - book.y) / book.height);
        return Mathf.Pow((normalized.x - .225f) / .12f, 2) + Mathf.Pow((normalized.y - .45f) / .18f, 2) <= 1;
    }

    private void Update()
    {
        if (!visible || loading) return;
        // Keep the opening visible even when first-use asset loading stalls a frame.
        screenAge += Mathf.Min(Time.unscaledDeltaTime, .1f);
        float age = screenAge;
        if (ending && age >= EndingHold && closeAt < 0) { closeAt = age; FirstLevelMusic.Cover(false); StorySfx.Play("BookClose", .7f); }
        if (!ending && closeAt < 0 && age > OpenDuration + .25f && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouse = Mouse.current.position.ReadValue(); mouse.y = Screen.height - mouse.y;
            if (OverPortrait(mouse, BookRect())) { closeAt = age; FirstLevelMusic.Cover(false); StorySfx.Play("Click", .32f); StorySfx.Play("BookClose", .65f); }
        }
        if (closeAt < 0 || age - closeAt < CloseDuration + (ending ? .8f : 0)) return;
        if (ending)
        {
            loading = true; gameStarted = false;
            // The tower owns the final pause; restore normal time before the new menu scene.
            Time.timeScale = 1;
            previousTimeScale = 1;
            SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        }
        else
        {
            gameStarted = true;
            Hide();
            Action begin = onStart; onStart = null; begin?.Invoke();
        }
    }

    private void Hide()
    {
        if (!visible) return;
        visible = false; IsShowing = false;
        FirstLevelMusic.Cover(false);
        Time.timeScale = previousTimeScale;
        if (player) player.isInteracting = previousInteraction;
    }

    private void CreateGlow()
    {
        const int size = 64;
        glow = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float radius = Vector2.Distance(new Vector2(x, y), Vector2.one * (size - 1) * .5f) / (size * .5f);
            pixels[y * size + x] = new Color(1, .72f, .34f, Mathf.Pow(Mathf.Clamp01(1 - radius), 2));
        }
        glow.SetPixels(pixels); glow.Apply(false, true);
    }

    private void OnGUI()
    {
        if (!visible || !artwork) return;
        Matrix4x4 saved = GUI.matrix; Color color = GUI.color; int depth = GUI.depth;
        try
        {
            GUI.matrix = Matrix4x4.identity; GUI.depth = -3000;
            GUI.color = new Color(.025f, .018f, .025f, 1);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            float age = screenAge;
            float closing = closeAt < 0 ? 0 : Mathf.Clamp01((age - closeAt) / CloseDuration);
            float opening = ending ? 1 : Mathf.Clamp01((age - .2f) / OpenDuration);
            float leftOpen = Mathf.SmoothStep(0, 1, Mathf.Clamp01(opening / .85f));
            float rightOpen = Mathf.SmoothStep(0, 1, Mathf.Clamp01((opening - .12f) / .88f));
            leftOpen *= 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((closing - .14f) / .86f));
            rightOpen *= 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(closing / .86f));
            float open = Mathf.Max(leftOpen, rightOpen);
            Rect book = BookRect();
            float half = book.width * .5f, spine = book.center.x;
            float left = half * Mathf.Max(.008f, Mathf.Sin(leftOpen * Mathf.PI * .5f));
            float right = half * Mathf.Max(.008f, Mathf.Sin(rightOpen * Mathf.PI * .5f));
            // Warm light behind the book and changing page shade reveal the hinge motion.
            GUI.color = new Color(1, .69f, .31f, .15f * open);
            GUI.DrawTexture(new Rect(book.x - book.width * .15f, book.y - book.height * .1f, book.width * 1.3f, book.height * 1.2f), glow);
            GUI.color = new Color(Mathf.Lerp(.4f, 1, leftOpen), Mathf.Lerp(.34f, 1, leftOpen), Mathf.Lerp(.28f, 1, leftOpen), Mathf.Clamp01(open * 8));
            GUI.DrawTextureWithTexCoords(new Rect(spine - left, book.y, left, book.height), artwork, new Rect(0, 0, .5f, 1));
            GUI.color = new Color(Mathf.Lerp(.4f, 1, rightOpen), Mathf.Lerp(.34f, 1, rightOpen), Mathf.Lerp(.28f, 1, rightOpen), Mathf.Clamp01(open * 8));
            GUI.DrawTextureWithTexCoords(new Rect(spine, book.y, right, book.height), artwork, new Rect(.5f, 0, .5f, 1));
            if (Mathf.Min(leftOpen, rightOpen) > .995f && closeAt < 0)
            {
                DrawTitleWave(book, age);
                if (!ending) DrawPortraitHint(book, age);
            }
        }
        finally { GUI.matrix = saved; GUI.color = color; GUI.depth = depth; }
    }

    private void DrawTitleWave(Rect book, float time)
    {
        // Only the printed heading band moves; portraits and page edges stay still.
        float left = ending ? .17f : .09f, right = ending ? .45f : .95f;
        float top = ending ? .23f : .09f, bottom = ending ? .76f : .225f;
        Rect band = new Rect(book.x + book.width * left, book.y + book.height * top, book.width * (right - left), book.height * (bottom - top));
        GUI.BeginGroup(band);
        const int slices = 120;
        for (int i = 0; i < slices; i++)
        {
            float u = i / (float)slices;
            float shift = Mathf.Sin(u * Mathf.PI * 3 + time * 1.3f) * book.height * .0035f;
            float shimmer = Mathf.Pow(.5f + .5f * Mathf.Cos(u * Mathf.PI * 2 - time * .8f), 6);
            GUI.color = new Color(1 + shimmer * .22f, 1 + shimmer * .13f, 1 + shimmer * .035f, 1);
            // Sample displaced UVs into fixed strips, avoiding gaps between strips.
            GUI.DrawTextureWithTexCoords(new Rect(u * band.width, 0, band.width / slices + .5f, band.height), artwork,
                new Rect(left + u * (right - left), 1 - bottom + shift / book.height, (right - left) / slices, bottom - top));
        }
        GUI.EndGroup();
        // A slow, feathered pool of golden light travels across the printed lettering.
        float lightX = band.x + band.width * (.5f + .42f * Mathf.Sin(time * .38f));
        float lightWidth = band.width * (ending ? .85f : .32f);
        GUI.color = new Color(1, .77f, .38f, .17f + .04f * Mathf.Sin(time * 1.2f));
        GUI.DrawTexture(new Rect(lightX - lightWidth * .5f, band.y - band.height * .35f, lightWidth, band.height * 1.7f), glow);
    }

    private void DrawPortraitHint(Rect book, float time)
    {
        Vector2 mouse = Event.current.mousePosition;
        bool hover = OverPortrait(mouse, book);
        float pulse = .5f + .5f * Mathf.Sin(time * 2);
        Vector2 center = new Vector2(book.x + book.width * .225f, book.y + book.height * .45f);
        // Breathing halo and orbiting fireflies replace a written button label.
        GUI.color = new Color(1, .62f, .27f, (hover ? .34f : .13f) + pulse * .08f);
        GUI.DrawTexture(new Rect(center.x - book.width * .16f, center.y - book.height * .24f, book.width * .32f, book.height * .48f), glow);
        for (int i = 0; i < 9; i++)
        {
            float angle = time * .23f + i * Mathf.PI * 2 / 9;
            float x = center.x + Mathf.Cos(angle) * book.width * .116f;
            float y = center.y + Mathf.Sin(angle) * book.height * .17f;
            float size = book.height * (hover ? .027f : .019f);
            GUI.color = new Color(1, .82f, .43f, (hover ? .95f : .55f) * (.7f + pulse * .3f));
            GUI.DrawTexture(new Rect(x - size * .5f, y - size * .5f, size, size), glow);
        }
    }

    private void OnDisable() { Hide(); }
    private void OnDestroy() { if (glow) Destroy(glow); }
}
