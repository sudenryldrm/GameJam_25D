using UnityEngine;

public static class GamePerformanceSettings
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureFrameRate()
    {
        // Avoid rendering unlimited frames between physics ticks on desktop.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
