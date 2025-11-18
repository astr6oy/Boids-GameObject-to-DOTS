using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Range(10, 120)]
    public int sampleCount = 60;

    [Range(12, 48)]
    public int fontSize = 24;

    public Vector2 offset = new Vector2(10f, 10f);

    private float[] frameTimes;
    private int currentIndex = 0;
    private int filledCount = 0;

    private float averageFPS;
    private float updateTimer = 0f;
    private const float updateInterval = 0.1f;

    private GUIStyle style;

    void Start()
    {
        frameTimes = new float[sampleCount];

        style = new GUIStyle();
        style.fontSize = fontSize;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.UpperRight;
    }

    void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        frameTimes[currentIndex] = deltaTime;
        currentIndex = (currentIndex + 1) % sampleCount;

        if (filledCount < sampleCount)
        {
            filledCount++;
        }

        updateTimer += deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            averageFPS = CalculateAverageFPS();
        }
    }

    float CalculateAverageFPS()
    {
        if (filledCount == 0)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < filledCount; i++)
        {
            sum += frameTimes[i];
        }

        float avgFrameTime = sum / filledCount;

        if (avgFrameTime > 0f)
        {
            return 1f / avgFrameTime;
        }

        return 0f;
    }

    void OnGUI()
    {
        float x = Screen.width - offset.x;
        float y = offset.y;

        Rect rect = new Rect(x - 150f, y, 150f, 30f);

        string fpsText = string.Format("FPS: {0:F1}", averageFPS);
        GUI.Label(rect, fpsText, style);
    }
}
