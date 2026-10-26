using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DefaultExecutionOrder(600)]
public sealed class MapOpeningFlythrough : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }
    private static bool completedThisRun;
    public bool Running { get; private set; }
    private CameraFollow25D cameraFollow;
    private Action finished;
    private GameObject[] prefabs;
    private GameObject preview, sunObject;
    private Material skyMaterial, originalSky;
    private readonly List<Light> disabledSuns = new List<Light>();
    private float beganAt;
    private float elapsed;
    private int shot = -1;
    private Color skyColor, equatorColor, groundColor, fogColor;
    private bool fogEnabled, restored;
    private FogMode fogMode;
    private AmbientMode ambientMode;
    private float fogStart, fogEnd;
    private const float Offset = 1000;
    private const float ShotDuration = 8f, ReturnDuration = 6f, TotalDuration = 30f;
    private GUIStyle narrationStyle;
    private static readonly string[] Narration = {
        "Bir varmış, bir yokmuş, evvel zaman içinde masalların en bilinen köşesinde her şey yazıldığı gibi başlarmış.",
        "Üvey anne aynaya bakar, avcı kıza kıyamaz, prenses ise yedi cücelerin evine sığınıp ta ki o zehirli elma uzatılana kadar her söylenene boyun eğermiş.",
        "Ama bu hikâye o ezberlenen masallardan değilmiş."
    };
    private readonly List<Renderer> hiddenPlayer = new List<Renderer>();
    private Transform player;
    private static readonly Vector3[][] Routes = {
        new[] { new Vector3(-44,2.15f,-4), new Vector3(-40,1.95f,-6), new Vector3(-36,1.55f,-8), new Vector3(-31,1.35f,-8.5f), new Vector3(-27,1.35f,-8.5f) },
        new[] { new Vector3(13.8f,6.4f,-1.8f), new Vector3(15.5f,5.5f,-1.8f), new Vector3(18,2.6f,-1.8f), new Vector3(21.5f,2.15f,-1.8f), new Vector3(25.5f,2.15f,-1.8f) },
        new[] { new Vector3(-8.5f,2.15f,-.4f), new Vector3(-5,3,-.4f), new Vector3(-1.5f,3.85f,-.4f), new Vector3(2,4.68f,-.4f), new Vector3(5.5f,5.5f,-.4f) }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetForNewGame() { completedThisRun = false; IsPlaying = false; }

    public bool TryPlay(CameraFollow25D follow, Action onFinished)
    {
        if (completedThisRun || !follow || gameObject.scene.name != "SampleScene")
        { Debug.Log("OPENING skipped: completed=" + completedThisRun + ", camera=" + (bool)follow + ", scene=" + gameObject.scene.name); return false; }
        prefabs = new[] { Resources.Load<GameObject>("MapPreviews/Forest"),
            Resources.Load<GameObject>("MapPreviews/WitchForest"), Resources.Load<GameObject>("MapPreviews/Tower") };
        foreach (GameObject prefab in prefabs)
            if (!prefab) { Debug.LogError("Opening tour previews need baking: Tools/Story/Bake Opening Map Previews"); return false; }
        cameraFollow = follow; finished = onFinished;
        var actor = FindFirstObjectByType<PlayerController25D>(); player = actor ? actor.transform : null;
        originalSky = RenderSettings.skybox;
        ambientMode = RenderSettings.ambientMode;
        skyColor = RenderSettings.ambientSkyColor; equatorColor = RenderSettings.ambientEquatorColor;
        groundColor = RenderSettings.ambientGroundColor;
        fogEnabled = RenderSettings.fog; fogMode = RenderSettings.fogMode;
        fogColor = RenderSettings.fogColor; fogStart = RenderSettings.fogStartDistance; fogEnd = RenderSettings.fogEndDistance;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional && light.enabled) { disabledSuns.Add(light); light.enabled = false; }
        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader) { skyMaterial = new Material(skyShader); skyMaterial.SetFloat("_SunSize", .025f); }
        sunObject = new GameObject("Opening Preview Sun");
        var sun = sunObject.AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(38, -32, 0);
        cameraFollow.BeginCameraSequence();
        Running = true; IsPlaying = true; completedThisRun = true; restored = false;
        FirstLevelMusic.Cover(true);
        beganAt = Time.unscaledTime; elapsed = 0;
        Debug.Log("OPENING started: three scenery maps, 30 seconds with story narration.");
        SetShot(0); Evaluate(0);
        return true;
    }

    private void SetShot(int value)
    {
        if (value == shot) return;
        shot = value;
        Debug.Log("OPENING shot " + shot + " at " + elapsed.ToString("0.00") + " seconds");
        if (preview) { preview.SetActive(false); Destroy(preview); preview = null; }
        if (shot == 3)
        {
            RestoreEnvironment();
            if (player) foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
                if (renderer.enabled) { hiddenPlayer.Add(renderer); renderer.enabled = false; }
            return;
        }
        preview = Instantiate(prefabs[shot], new Vector3(Offset, 0, 0), Quaternion.identity);
        if (shot < 2) { ForestWind.ApplyHierarchy(preview); OpeningForestScenery.Build(preview, shot == 1); }
        MapPreviewInfo info = preview.GetComponent<MapPreviewInfo>();
        Light sun = sunObject.GetComponent<Light>();
        sun.color = shot == 0 ? new Color(1f, 211f / 255f, 144f / 255f) : info.sunlight;
        sun.intensity = shot == 0 ? 1.5625f : info.intensity;
        sun.useColorTemperature = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = info.sky;
        RenderSettings.ambientEquatorColor = info.ambient;
        RenderSettings.ambientGroundColor = info.ambient * .55f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = info.mist; RenderSettings.fogStartDistance = 45; RenderSettings.fogEndDistance = 150;
        if (skyMaterial)
        {
            skyMaterial.SetColor("_SkyTint", info.sky);
            skyMaterial.SetColor("_GroundColor", info.mist);
            RenderSettings.skybox = skyMaterial;
        }
    }

    private void Update()
    {
        if (!Running) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Complete(); return; }
        // Do not consume the movie while a first-use shader or preview is blocking a frame.
        elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
        float time = elapsed;
        if (time >= TotalDuration) { Complete(); return; }
        Evaluate(time);
    }

    private void Evaluate(float time)
    {
        int next = Mathf.Min(3, (int)(time / ShotDuration));
        SetShot(next);
        float t = Mathf.SmoothStep(0, 1, next < 3 ? (time - next * ShotDuration) / ShotDuration : (time - ShotDuration * 3) / ReturnDuration);
        Vector3 position, look;
        if (next < 3)
        {
            position = Sample(Routes[next], t) + Vector3.right * Offset;
            position.y += Mathf.Sin(t * Mathf.PI * 4) * .025f;
            Vector3 direction = next == 2 ? new Vector3(4, .8f, 3.8f) : new Vector3(7, -.12f, next == 0 ? 1.8f : 2.6f);
            look = position + direction;
            cameraFollow.SetCinematicPose(position, Quaternion.LookRotation(look - position), 10, true, 74);
        }
        else
        {
            Vector3 home = cameraFollow.CinematicHome;
            Vector3 eye = player ? player.position + Vector3.up * .65f : home + Vector3.forward * 16;
            position = Vector3.Lerp(eye, home, t);
            Quaternion rotation = Quaternion.Slerp(Quaternion.LookRotation(new Vector3(7,0,1.8f)), cameraFollow.CinematicHomeRotation, t);
            float depth = Mathf.Abs(Vector3.Dot(eye-home, cameraFollow.CinematicHomeRotation * Vector3.forward));
            float endFov = 2 * Mathf.Atan(cameraFollow.CinematicHomeSize / Mathf.Max(1, depth)) * Mathf.Rad2Deg;
            cameraFollow.SetCinematicPose(position, rotation, cameraFollow.CinematicHomeSize, true, Mathf.Lerp(74, endFov, t));
            if (t > .4f) RestorePlayer();
        }
    }

    private static Vector3 Sample(Vector3[] points, float time)
    {
        float position = Mathf.Clamp01(time) * (points.Length-1);
        int i = Mathf.Min(points.Length-2, Mathf.FloorToInt(position));
        float t = position-i;
        Vector3 a=points[Mathf.Max(0,i-1)], b=points[i], c=points[i+1], d=points[Mathf.Min(points.Length-1,i+2)];
        return .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
    }

    private void RestorePlayer()
    { foreach (Renderer renderer in hiddenPlayer) if (renderer) renderer.enabled=true; hiddenPlayer.Clear(); }

    private void RestoreEnvironment()
    {
        if (restored) return;
        restored = true;
        if (sunObject) sunObject.SetActive(false);
        foreach (Light light in disabledSuns) if (light) light.enabled = true;
        RenderSettings.skybox = originalSky; RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = skyColor; RenderSettings.ambientEquatorColor = equatorColor;
        RenderSettings.ambientGroundColor = groundColor;
        RenderSettings.fog = fogEnabled; RenderSettings.fogMode = fogMode; RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd;
    }

    private void Complete()
    {
        if (!Running) return;
        Running = false; IsPlaying = false;
        FirstLevelMusic.Cover(false);
        Debug.Log("OPENING completed at " + elapsed.ToString("0.00") + " seconds");
        RestoreEnvironment();
        RestorePlayer();
        if (preview) { preview.SetActive(false); Destroy(preview); }
        if (sunObject) Destroy(sunObject);
        if (skyMaterial) Destroy(skyMaterial);
        if (cameraFollow) cameraFollow.SetCinematicPose(cameraFollow.CinematicHome, cameraFollow.CinematicHomeRotation, cameraFollow.CinematicHomeSize);
        Action callback = finished; finished = null; callback?.Invoke();
    }

    private void OnDisable() { finished = null; Complete(); }
    public void Cancel() { finished = null; Complete(); }

    private void OnGUI()
    {
        if (!Running) return;
        Matrix4x4 matrix = GUI.matrix; Color color = GUI.color; int depth = GUI.depth;
        GUI.matrix = Matrix4x4.identity; GUI.depth = -1500;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height * .065f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height * .935f, Screen.width, Screen.height * .065f), Texture2D.whiteTexture);
        float time = elapsed;
        DrawNarration(time);
        float local = time - shot * ShotDuration;
        float fade = Mathf.Clamp01(1 - local / .22f);
        if (shot < 3) fade = Mathf.Max(fade, Mathf.Clamp01((local - (ShotDuration - .22f)) / .22f));
        GUI.color = new Color(0, 0, 0, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.matrix = matrix; GUI.color = color; GUI.depth = depth;
    }

    private void DrawNarration(float time)
    {
        int index = time < 9 ? 0 : time < 23 ? 1 : 2;
        float start = index == 0 ? .4f : index == 1 ? 9 : 23;
        float end = index == 0 ? 9 : index == 1 ? 23 : TotalDuration;
        float alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - start) / 1.2f)) *
            Mathf.SmoothStep(0, 1, Mathf.Clamp01((end - time) / .9f));
        if (alpha <= 0) return;
        if (narrationStyle == null)
            narrationStyle = new GUIStyle { font = StorybookUI.RegularFont, fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
        narrationStyle.normal.textColor = Color.white;
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        narrationStyle.fontSize = Mathf.Max(18, Mathf.RoundToInt((index == 2 ? 54 : 40) * scale));
        string text = index == 0 ? "<size=" + Mathf.RoundToInt(narrationStyle.fontSize * 1.65f) + "><color=#EAC378>B</color></size>" + Narration[index].Substring(1) :
            index == 2 ? "Ama bu hikâye\no ezberlenen masallardan <color=#EAC378>değilmiş.</color>" : Narration[index];
        float width = Screen.width * (index == 2 ? .68f : .72f);
        float height = narrationStyle.CalcHeight(new GUIContent(text), width);
        float y = Screen.height * (index == 2 ? .49f : .72f) - height * .5f + (1 - alpha) * 24 * scale;
        // Soft vignette and drifting golden motes frame the story without obscuring the tour.
        GUI.color = new Color(.025f, .015f, .035f, alpha * (index == 2 ? .36f : .12f));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        for (int i = 0; i < 20; i++)
        {
            float edge = (20 - i) / 20f;
            GUI.color = new Color(.02f, .01f, .025f, alpha * edge * .035f);
            float inset = i * Screen.width * .008f;
            GUI.DrawTexture(new Rect(inset, 0, Screen.width * .008f + 1, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - inset - Screen.width * .008f, 0, Screen.width * .008f + 1, Screen.height), Texture2D.whiteTexture);
        }
        for (int i = 0; i < 18; i++)
        {
            float x = Mathf.Repeat(i * .618034f, 1) * Screen.width;
            float py = Mathf.Repeat(i * .137f - time * (.006f + (i % 3) * .002f), 1) * Screen.height;
            float shimmer = .5f + .5f * Mathf.Sin(time * 1.5f + i * 2.4f);
            GUI.color = new Color(1, .77f, .38f, alpha * shimmer * .35f);
            DrawDiamond(x, py, (2 + i % 3) * scale);
        }
        // A feathered dark backing keeps the manuscript readable over moving scenery.
        for (int i = 0; i < 12; i++)
        {
            float padding = (16 + i * 5) * scale;
            GUI.color = new Color(0.025f, .018f, .012f, alpha * .045f);
            GUI.DrawTexture(new Rect(0, y - padding, Screen.width, height + padding * 2), Texture2D.whiteTexture);
        }
        Rect rect = new Rect((Screen.width - width) * .5f, y, width, height);
        GUI.color = new Color(0, 0, 0, alpha * .9f);
        GUI.Label(new Rect(rect.x + 2 * scale, rect.y + 3 * scale, rect.width, rect.height), text, narrationStyle);
        GUI.color = new Color(1f, .91f, .73f, alpha);
        GUI.Label(rect, text, narrationStyle);
        GUI.color = new Color(.92f, .73f, .4f, alpha * .8f);
        float center = Screen.width * .5f;
        float ornamentY = y - 28 * scale;
        float reach = 180 * scale * Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - start) / 1.6f));
        GUI.DrawTexture(new Rect(center - reach, ornamentY, Mathf.Max(0, reach - 14 * scale), Mathf.Max(1, scale)), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(center + 14 * scale, ornamentY, Mathf.Max(0, reach - 14 * scale), Mathf.Max(1, scale)), Texture2D.whiteTexture);
        DrawDiamond(center, ornamentY, 6 * scale);
        DrawDiamond(center - 18 * scale, y + height + 25 * scale, 2.5f * scale);
        DrawDiamond(center, y + height + 25 * scale, 4 * scale);
        DrawDiamond(center + 18 * scale, y + height + 25 * scale, 2.5f * scale);
    }

    private static void DrawDiamond(float x, float y, float radius)
    {
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(45, new Vector2(x, y));
        GUI.DrawTexture(new Rect(x - radius * .5f, y - radius * .5f, radius, radius), Texture2D.whiteTexture);
        GUI.matrix = saved;
    }
}
