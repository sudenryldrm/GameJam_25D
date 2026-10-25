using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class CastleInteriorLighting : MonoBehaviour
{
    private GameObject accents;
    private Light leftTorch, rightTorch, moonlight;
    private Camera view;
    private int floor = -1;
    private float nextCheck;

    private void OnEnable() { Apply(); }

    public void Apply()
    {
        if (SceneManager.GetActiveScene() != gameObject.scene || gameObject.scene.name != "Scene3_TowerEscape") return;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.gameObject.scene == gameObject.scene && light.type == LightType.Directional)
            {
                light.color = new Color(.74f, .82f, 1);
                light.useColorTemperature = false;
                light.intensity = 1.05f;
                light.transform.rotation = Quaternion.Euler(32, -22, 0);
                light.shadows = LightShadows.Soft; light.shadowStrength = .45f;
            }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.34f, .4f, .53f);
        RenderSettings.ambientEquatorColor = new Color(.23f, .25f, .31f);
        RenderSettings.ambientGroundColor = new Color(.13f, .15f, .2f);
        RenderSettings.reflectionIntensity = .4f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(.11f, .14f, .22f);
        RenderSettings.fogStartDistance = 25; RenderSettings.fogEndDistance = 85;
        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (camera.gameObject.scene == gameObject.scene && camera.CompareTag("MainCamera")) { view = camera; break; }
        if (view) { view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.04f, .05f, .08f); }
        if (!accents)
        {
            accents = new GameObject("Castle Local Lighting") { hideFlags = HideFlags.DontSave };
            accents.transform.SetParent(transform, false);
            leftTorch = MakeLight("Warm Torch Left", LightType.Point, new Color(1, .46f, .16f), 3.2f, 7);
            rightTorch = MakeLight("Warm Torch Right", LightType.Point, new Color(1, .46f, .16f), 3.2f, 7);
            moonlight = MakeLight("Window Moonlight", LightType.Spot, new Color(.43f, .65f, 1), 2.3f, 10);
            moonlight.spotAngle = 90; moonlight.innerSpotAngle = 45;
            moonlight.transform.rotation = Quaternion.Euler(25, 180, 0);
        }
        floor = -1;
        UpdateFloor();
    }

    private Light MakeLight(string name, LightType type, Color color, float intensity, float range)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(accents.transform, false);
        var light = go.AddComponent<Light>();
        light.type = type; light.color = color; light.intensity = intensity; light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < nextCheck) return;
        nextCheck = Time.realtimeSinceStartup + .2f;
        UpdateFloor();
    }

    private void UpdateFloor()
    {
        if (!accents || !view) return;
        int next = Mathf.Clamp(Mathf.FloorToInt((view.transform.position.y - 2f) / 6.5f), 0, 6);
        if (next == floor) return;
        floor = next;
        float y = floor * 6.5f;
        // Reuse three lights around the visible floor instead of keeping lights on every floor.
        leftTorch.transform.position = new Vector3(-4.4f, y + 4, 1.8f);
        rightTorch.transform.position = new Vector3(4.4f, y + 4, 1.8f);
        moonlight.transform.position = new Vector3(0, y + 4.3f, 3.3f);
    }

    private void OnDisable()
    {
        if (!accents) return;
        if (Application.isPlaying) Destroy(accents); else DestroyImmediate(accents);
    }
}
